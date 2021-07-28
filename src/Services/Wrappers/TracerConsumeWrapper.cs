using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Services.Wrappers
{
    public sealed class TracerConsumeWrapper<TMessage> : IConsumeWrapper<TMessage> where TMessage : class
    {
        private readonly ITracer _tracer;
        private readonly ILogger _logger;

        public TracerConsumeWrapper(ITracer tracer, ILogger logger)
        {
            _tracer = tracer;
            _logger = logger;
        }

        public async Task WrapPipe(IMessage<TMessage> message, IConsumePipe<TMessage> pipe)
        {
            var stringDictionary = message.Properties.Headers.Where(x => x.Value.GetType() == typeof(byte[])).ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[])x.Value));
            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

            using (_tracer.BuildSpan(nameof(WrapPipe)).AddReference(References.FollowsFrom, spanContext).StartActive(true))
            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId), _tracer.ActiveSpan.Context.TraceId) }))
            {
                var tryCount = 0;

                //TODO надо фильтровать ошибки базы, с3 и реббита, но тогда цикл должен быть выше по абстракции
                while (true)
                    try
                    {
                        await pipe.Pipe(message.Body).ConfigureAwait(false);
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