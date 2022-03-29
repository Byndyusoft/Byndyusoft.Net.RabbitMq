using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Tracing
{
    /// <summary>
    ///     Middleware for tracing of consuming messages
    /// </summary>
    /// <typeparam name="TMessage">Consuming message type</typeparam>
    public sealed class TracerConsumeMiddleware<TMessage> : IConsumeMiddleware<TMessage> where TMessage : class
    {
        private readonly ILogger<TracerConsumeMiddleware<TMessage>> _logger;
        private readonly ITracer _tracer;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="tracer">Tracer</param>
        /// <param name="logger">Logger</param>
        public TracerConsumeMiddleware(ITracer tracer, ILogger<TracerConsumeMiddleware<TMessage>> logger)
        {
            _tracer = tracer;
            _logger = logger;
        }

        /// <summary>
        ///     Adds tracing around message consuming
        /// </summary>
        /// <param name="message">Consuming message</param>
        /// <param name="next">Next middleware in a chain</param>
        public async Task Handle(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next)
        {
            var stringDictionary = message.Properties.Headers.Where(x => x.Value.GetType() == typeof(byte[]))
                .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[]) x.Value));
            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

            using (_tracer.BuildSpan(nameof(Handle)).AddReference(References.FollowsFrom, spanContext)
                .StartActive(true))
            using (_logger.BeginScope(new[]
            {
                new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId),
                    _tracer.ActiveSpan.Context.TraceId)
            }))
            {
                var tryCount = 0;

                //TODO надо фильтровать ошибки базы, с3 и реббита, но тогда цикл должен быть выше по абстракции
                while (true)
                    try
                    {
                        await next(message);
                        break;
                    }
                    catch (ProcessMessageException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        tryCount++;
                        _logger.LogWarning(e, $"Process error, try count {tryCount}");

                        if (tryCount >= 5)
                            throw;
                    }
            }
        }
    }
}