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
using OpenTracing.Tag;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Tracing
{
    /// <summary>
    ///     Middleware for tracing of returned messages
    /// </summary>
    /// <typeparam name="TMessage">Returned message type</typeparam>
    public sealed class TraceReturnedMiddleware<TMessage> : IReturnedMiddleware<TMessage> where TMessage : class
    {
        private readonly ILogger<TraceReturnedMiddleware<TMessage>> _logger;
        private readonly ITracer _tracer;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="tracer">Tracer</param>
        /// <param name="logger">Logger</param>
        public TraceReturnedMiddleware(ITracer tracer, ILogger<TraceReturnedMiddleware<TMessage>> logger)
        {
            _tracer = tracer;
            _logger = logger;
        }

        /// <summary>
        ///     Adds tracing around returned message handling
        /// </summary>
        /// <param name="args">Info regarding returned message</param>
        /// <param name="next">Next middleware in a chain</param>
        public async Task Handle(MessageReturnedEventArgs args, Func<MessageReturnedEventArgs, Task> next)
        {
            var stringDictionary = args.MessageProperties.Headers.Where(x => x.Value.GetType() == typeof(byte[]))
                .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[]) x.Value));
            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

            using (_tracer.BuildSpan(nameof(Handle)).AddReference(References.ChildOf, spanContext).StartActive(true))
            using (_logger.BeginScope(new[]
            {
                new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId),
                    _tracer.ActiveSpan.Context.TraceId)
            }))
            {
                _tracer.ActiveSpan.SetTag(Tags.Error, true);

                if (args.MessageProperties.Headers.TryGetValue(Consts.MessageKeyHeader, out var bytes) &&
                    bytes is byte[] value)
                {
                    var key = Encoding.UTF8.GetString(value);

                    _logger.LogError("Message returned {Exchange} {RoutingKey} reason {ReturnReason} {Key}",
                        args.MessageReturnedInfo.Exchange,
                        args.MessageReturnedInfo.RoutingKey,
                        args.MessageReturnedInfo.ReturnReason,
                        key);

                    await next(args);
                }
                else
                {
                    _logger.LogError("Can not get error message filename");
                }
            }
        }
    }
}