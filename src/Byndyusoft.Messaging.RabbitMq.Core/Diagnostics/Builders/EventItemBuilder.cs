using Byndyusoft.Messaging.RabbitMq.Serialization;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics.Builders
{
    public class EventItemBuilder
    {
        private const string ExchangeDescription = "Exchange";
        private const string RoutingKeyDescription = "RoutingKey";
        private const string QueueDescription = "Queue";
        private const string ContentDescription = "RoutingKey";
        private const string PropertiesDescription = "RoutingKey";

        public static EventItem[]? BuildFromMessagePublishing(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is not RabbitMqMessage message)
                return null;

            var eventItems = new EventItem[]
            {
                new("amqp.message.exchange", message.Exchange ?? string.Empty, ExchangeDescription),
                new("amqp.message.routing_key", message.RoutingKey, RoutingKeyDescription),
                new("amqp.message.mandatory", message.Mandatory, "Mandatory"),
                new("amqp.message.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult(), ContentDescription),
                new("amqp.message.properties", JsonSerializer.Serialize(message.Properties, options), PropertiesDescription)
            };

            return eventItems;
        }

        public static EventItem[]? BuildFromMessageReturned(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is not ReturnedRabbitMqMessage message)
                return null;

            var eventItems = new EventItem[]
            {
                new("amqp.message.exchange", message.Exchange ?? string.Empty, ExchangeDescription),
                new("amqp.message.routing_key", message.RoutingKey, RoutingKeyDescription),
                new("amqp.message.return_reason", message.ReturnReason, "ReturnReason"),
                new("amqp.message.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    ContentDescription),
                new("amqp.message.properties", JsonSerializer.Serialize(message.Properties, options),
                    PropertiesDescription)
            };

            return eventItems;
        }

        public static EventItem[]? BuildFromMessageGot(object? payload, RabbitMqDiagnosticsOptions options)
        {
            return BuildFromMessageConsuming(payload, options);
        }

        public static EventItem[]? BuildFromMessageReplied(object? payload, RabbitMqDiagnosticsOptions options)
        {
            return BuildFromMessageConsuming(payload, options);
        }

        public static EventItem[]? BuildFromMessageConsumed(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is not ConsumeResult result)
                return null;

            var eventItems = new EventItem[]
            {
                new("result", result.GetDescription(), "Result")
            };

            return eventItems;
        }

        private static EventItem[]? BuildFromMessageConsuming(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is null)
            {
                return new EventItem[]
                {
                    new("amqp.message", null, "Message")
                };
            }

            if (payload is not ReceivedRabbitMqMessage message)
                return null;

            var eventItems = new EventItem[]
            {
                new("amqp.message.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    ContentDescription),
                new("amqp.message.exchange", message.Exchange, ExchangeDescription),
                new("amqp.message.queue", message.Queue, QueueDescription),
                new("amqp.message.routing_key", message.RoutingKey, RoutingKeyDescription),
                new("amqp.message.delivery_tag", message.DeliveryTag, "DeliveryTag"),
                new("amqp.message.redelivered", message.Redelivered, "Redelivered"),
                new("amqp.message.consumer_tag", message.ConsumerTag, "ConsumerTag"),
                new("amqp.message.retry_count", message.RetryCount, "RetryCount"),
                new("amqp.message.properties", JsonSerializer.Serialize(message.Properties, options),
                    PropertiesDescription)
            };

            return eventItems;
        }
    }
}