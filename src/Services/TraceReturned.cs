using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Byndyusoft.Net.RabbitMq.Services
{
    public sealed class TraceReturned<TMessage> : IReturnedPipe<TMessage> where TMessage : class
    {
        private readonly ITracer _tracer;
        private readonly ILogger _logger;

        public TraceReturned(ITracer tracer, ILogger logger)
        {
            _tracer = tracer;
            _logger = logger;
        }

        public Task<MessageReturnedEventArgs> Pipe(MessageReturnedEventArgs args)
        {
            var stringDictionary = args.MessageProperties.Headers.Where(x => x.Value.GetType() == typeof(byte[])).ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[])x.Value));
            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

            using (_tracer.BuildSpan(nameof(Pipe)).AddReference(References.ChildOf, spanContext).StartActive(true))
            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId), _tracer.ActiveSpan.Context.TraceId) }))
            {
                _tracer.ActiveSpan.SetTag(Tags.Error, true);

                if (args.MessageProperties.Headers.TryGetValue(Consts.MessageKeyHeader, out var bytes) && bytes is byte[] value)
                {
                    var key = Encoding.UTF8.GetString(value);

                    _logger.LogError("Message returned {Exchange} {RoutingKey} reason {ReturnReason} {Key}",
                        args.MessageReturnedInfo.Exchange,
                        args.MessageReturnedInfo.RoutingKey,
                        args.MessageReturnedInfo.ReturnReason,
                        key);
                }
                else
                {
                    _logger.LogError("Can not get error message filename");
                }

                return Task.FromResult(args);
            }
        }
    }
}