using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Extensions;
using Byndyusoft.Net.RabbitMq.Models;
using Microsoft.Extensions.Logging;
using OpenTracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byndyusoft.Net.RabbitMq
{
    public class RabbitMqConsumer : IConsumer
    {
        private readonly PersistentConnection _connection;
        private readonly ILogger<RabbitMqConsumer> _logger;

        private readonly MessageHandlerMediator _mediator;
        private readonly object _mutex = new object();

        private readonly ITracer _tracer;
        private IModel _channel;

        private string _exchangeName;
        private string _queueName;
        private const string ErrorQueueKey = "error";

        public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger,
            ITracer tracer,
            PersistentConnection connection,
            MessageHandlerMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public void Subscribe(params ISubscription[] subscriptions)
        {
            InitChannel();

            foreach (var subscription in subscriptions)
            {
                _channel.QueueBind(_queueName, _exchangeName, subscription.RoutingKey);
                _mediator.Add(subscription.RoutingKey, subscription.Handler);
            }
        }

        public void Listen(Exchange exchange, string queueName)
        {
            InitChannel();

            _channel.ExchangeDeclare(exchange.Name, 
                exchange.Type, 
                exchange.IsDurable);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnConsumerReceived;
            consumer.Shutdown += OnConsumerShutdown;

            _channel.QueueDeclare(queueName, true, false, false, null);

            var errorQueueName = $"{queueName}.errors";
            _channel.QueueDeclare(errorQueueName, true, false, false, null);
            _channel.QueueBind(errorQueueName, exchange.Name, ErrorQueueKey);

            _channel.BasicConsume(queueName, false, consumer);

            _exchangeName = exchange.Name;
            _queueName = queueName;
        }

        public void Dispose()
        {
            _channel?.Close();
        }

        private async Task OnConsumerShutdown(object sender, ShutdownEventArgs @event)
        {
            _logger.LogInformation("Consumer was shutdown {ReasonShutdown}", @event.ReplyText);
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs @event)
        {
            InitChannel();

            var messageBody = @event.Body.ToArray();
            try
            {
                _channel.BasicAck(@event.DeliveryTag, false);

                var spanContext = _tracer.CreateSpanContextFromHeaders(@event.BasicProperties.Headers);
                var traceId = _tracer.ActiveSpan?.Context.TraceId;

                using (_tracer.BuildSpan(nameof(OnConsumerReceived))
                    .AddReference(References.FollowsFrom, spanContext)
                    .StartActive(true))
                using (_logger.BeginScope(new[] {new KeyValuePair<string, object>(nameof(traceId), traceId)}))
                {
                    var message = Encoding.UTF8.GetString(messageBody);
                    await _mediator.HandleAsync(@event.RoutingKey, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while message processed");

                var properties = _channel.CreateBasicProperties();
                properties.ContentType = "application/json";
                properties.DeliveryMode = 2;
                properties.Persistent = true;

                //TODO: ExceptionHandler
                _channel.BasicPublish(_exchangeName,
                    ErrorQueueKey,
                    true,
                    properties,
                    messageBody);
            }
        }

        private void InitChannel()
        {
            if (_channel != null)
                return;

            lock (_mutex)
            {
                if (_channel == null)
                    _channel = _connection.CreateChannel();
            }
        }
    }
}