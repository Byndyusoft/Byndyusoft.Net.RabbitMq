using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Propagation;
using RabbitMQ.Client;

namespace Byndyusoft.Net.RabbitMq
{
    public class RabbitMqProducer : IProducer
    {
        private readonly PersistentConnection _connection;

        private readonly ILogger<RabbitMqProducer> _logger;
        private readonly ITracer _tracer;

        public RabbitMqProducer(
            ILogger<RabbitMqProducer> logger,
            PersistentConnection connection,
            ITracer tracer
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        //TODO: BasicReturnEventHandler
        //TODO: channel и многопоточность - как не создавать каждый раз channel?
        public async Task PublishAsync<T>(Exchange exchange, string routingKey, T message,
            Dictionary<string, string> headers = null)
        {
            var span = _tracer.BuildSpan(nameof(PublishAsync)).Start();
            span.SetTag(nameof(message), JsonConvert.SerializeObject(message));

            try
            {
                using (var channel = _connection.CreateChannel())
                {
                    channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.IsDurable);

                    var properties = channel.CreateBasicProperties();
                    properties.ContentType = "application/json";
                    properties.DeliveryMode = 2;
                    properties.Persistent = true;

                    _tracer.Inject(_tracer.ActiveSpan.Context, BuiltinFormats.HttpHeaders,
                        new HttpHeadersCarrier(properties.Headers));

                    if (headers != null)
                        foreach (var header in headers)
                            properties.Headers.Add(header.Key, header.Value);

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                    channel.BasicPublish(exchange.Name, routingKey, true, properties, body);
                }
            }
            finally
            {
                span.Finish();
            }
        }
    }
}