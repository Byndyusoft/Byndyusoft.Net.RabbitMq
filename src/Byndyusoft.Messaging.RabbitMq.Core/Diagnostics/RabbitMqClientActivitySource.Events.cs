using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Serialization;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public class RabbitMqClientActivitySourceEvents
        {
            private readonly RabbitMqClientActivitySource _activitySource;

            public RabbitMqClientActivitySourceEvents(RabbitMqClientActivitySource activitySource)
            {
                _activitySource = activitySource;
            }

            public void MessagePublishing(Activity? activity, RabbitMqMessage message)
            {
                if (activity is null)
                    return;

                var tags = GetPublishedMessageEventTags(message);
                var activityEvent = new ActivityEvent("message.publishing", tags: tags);
                activity.AddEvent(activityEvent);
            }

            public void MessageReturned(Activity? activity, ReturnedRabbitMqMessage message)
            {
                if (activity is null)
                    return;

                var tags = GetReturnedMessageEventTags(message);
                var activityEvent = new ActivityEvent("message.returned", tags: tags);
                activity.AddEvent(activityEvent);
            }

            public void MessageGot(Activity? activity, ReceivedRabbitMqMessage? message)
            {
                if (activity is null)
                    return;

                var tags = GetConsumedMessageEventTags(message);
                var activityEvent = new ActivityEvent("message.got", tags: tags);
                activity.AddEvent(activityEvent);
            }

            public void MessageReplied(Activity? activity, ReceivedRabbitMqMessage message)
            {
                if (activity is null)
                    return;

                var tags = GetConsumedMessageEventTags(message);
                var activityEvent = new ActivityEvent("message.replied", tags: tags);
                activity.AddEvent(activityEvent);
            }

            public void MessageConsumed(Activity? activity, ReceivedRabbitMqMessage _, ConsumeResult result)
            {
                if (activity is null)
                    return;

                var tags = new ActivityTagsCollection {{"result", result.GetDescription()}};
                var activityEvent = new ActivityEvent("message.consumed", tags: tags);
                activity.AddEvent(activityEvent);
            }

            private ActivityTagsCollection GetReturnedMessageEventTags(ReturnedRabbitMqMessage message)
            {
                var tags = new ActivityTagsCollection();

                // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
                // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
                tags.Add("messaging.operation", "return");
                tags.Add("messaging.system", "rabbitmq");
                tags.Add("messaging.message.client_id", message.Properties.AppId);
                tags.Add("messaging.message.id", message.Properties.MessageId);
                tags.Add("messaging.message.conversation_id", message.Properties.CorrelationId);
                tags.Add("messaging.message.body",
                    message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                tags.Add("messaging.message.body.size",
                    message.Content.Headers.ContentLength);
                tags.Add("messaging.rabbitmq.message.properties",
                    JsonSerializer.Serialize(message.Properties, _activitySource._options));
                tags.Add("messaging.rabbitmq.destination.exchange", message.Exchange ?? string.Empty);
                tags.Add("messaging.rabbitmq.destination.routing_key", message.RoutingKey);
                tags.Add("messaging.rabbitmq.return.reason", message.ReturnReason);

                return tags;
            }

            private ActivityTagsCollection GetPublishedMessageEventTags(RabbitMqMessage message)
            {
                var tags = new ActivityTagsCollection();

                // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
                // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
                tags.Add("messaging.operation", "publish");
                tags.Add("messaging.system", "rabbitmq");
                tags.Add("messaging.message.client_id", message.Properties.AppId);
                tags.Add("messaging.message.id", message.Properties.MessageId);
                tags.Add("messaging.message.conversation_id", message.Properties.CorrelationId);
                tags.Add("messaging.message.body",
                    message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                tags.Add("messaging.message.body.size",
                    message.Content.Headers.ContentLength);
                tags.Add("messaging.rabbitmq.message.properties",
                    JsonSerializer.Serialize(message.Properties, _activitySource._options));
                tags.Add("messaging.rabbitmq.destination.exchange", message.Exchange ?? string.Empty);
                tags.Add("messaging.rabbitmq.destination.routing_key", message.RoutingKey);
                tags.Add("messaging.rabbitmq.message.mandatory", message.Mandatory);

                return tags;
            }

            private ActivityTagsCollection GetConsumedMessageEventTags(ReceivedRabbitMqMessage? message)
            {
                var tags = new ActivityTagsCollection();

                if (message is null)
                {
                    tags.Add("messaging.message", "null");
                }
                else
                {
                    // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
                    // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
                    tags.Add("messaging.operation", "receive");
                    tags.Add("messaging.system", "rabbitmq");
                    tags.Add("messaging.message.client_id", message.Properties.AppId);
                    tags.Add("messaging.message.id", message.Properties.MessageId);
                    tags.Add("messaging.message.conversation_id", message.Properties.CorrelationId);
                    tags.Add("messaging.message.body", 
                        message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    tags.Add("messaging.message.body.size",
                        message.Content.Headers.ContentLength);
                    tags.Add("messaging.rabbitmq.message.properties",
                        JsonSerializer.Serialize(message.Properties, _activitySource._options));
                    tags.Add("messaging.rabbitmq.destination.exchange", message.Exchange ?? string.Empty);
                    tags.Add("messaging.rabbitmq.destination.routing_key", message.RoutingKey);
                    tags.Add("messaging.rabbitmq.message.delivery_tag", message.DeliveryTag);
                    tags.Add("messaging.rabbitmq.message.redelivered", message.Redelivered);
                    tags.Add("messaging.rabbitmq.message.consumer_tag", message.ConsumerTag);
                    tags.Add("messaging.rabbitmq.message.retry_count", message.RetryCount);
                }

                return tags;
            }
        }
    }
}