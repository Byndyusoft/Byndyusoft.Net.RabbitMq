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

                ActivityContextPropagation.ExtractContext(activity, message?.Headers);

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

                tags.Add("amqp.message.exchange", message.Exchange ?? string.Empty);
                tags.Add("amqp.message.routing_key", message.RoutingKey);
                tags.Add("amqp.message.return_reason", message.ReturnReason);
                tags.Add("amqp.message.content",
                    message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                tags.Add("amqp.message.properties",
                    JsonSerializer.Serialize(message.Properties, _activitySource._options));

                return tags;
            }

            private ActivityTagsCollection GetPublishedMessageEventTags(RabbitMqMessage message)
            {
                var tags = new ActivityTagsCollection();

                tags.Add("amqp.message.exchange", message.Exchange ?? string.Empty);
                tags.Add("amqp.message.routing_key", message.RoutingKey);
                tags.Add("amqp.message.mandatory", message.Mandatory);
                tags.Add("amqp.message.content",
                    message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                tags.Add("amqp.message.properties",
                    JsonSerializer.Serialize(message.Properties, _activitySource._options));

                return tags;
            }

            private ActivityTagsCollection GetConsumedMessageEventTags(ReceivedRabbitMqMessage? message)
            {
                var tags = new ActivityTagsCollection();

                if (message is null)
                {
                    tags.Add("amqp.message", "null");
                }
                else
                {
                    tags.Add("amqp.message.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    tags.Add("amqp.message.exchange", message.Exchange);
                    tags.Add("amqp.message.queue", message.Queue);
                    tags.Add("amqp.message.routing_key", message.RoutingKey);
                    tags.Add("amqp.message.delivery_tag", message.DeliveryTag);
                    tags.Add("amqp.message.redelivered", message.Redelivered);
                    tags.Add("amqp.message.consumer_tag", message.ConsumerTag);
                    tags.Add("amqp.message.retry_count", message.RetryCount);
                    tags.Add("amqp.message.properties",
                        JsonSerializer.Serialize(message.Properties, _activitySource._options));
                }

                return tags;
            }
        }
    }
}