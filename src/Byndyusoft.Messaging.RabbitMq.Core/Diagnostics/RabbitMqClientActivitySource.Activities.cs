using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public class RabbitMqClientActivitySourceActivities
        {
            private readonly RabbitMqClientActivitySource _activitySource;

            public RabbitMqClientActivitySourceActivities(RabbitMqClientActivitySource activitySource)
            {
                _activitySource = activitySource;
            }

            public Activity? StartPublishMessage(RabbitMqEndpoint endpoint, RabbitMqMessage message)
            {
                Preconditions.CheckNotNull(endpoint, nameof(endpoint));
                Preconditions.CheckNotNull(message, nameof(message));

                var activity = _activitySource.StartActivity("Publish", endpoint, ActivityKind.Producer);
                if (activity is null)
                    return activity;

                _activitySource.SetMessageTags(activity, message);
                ActivityContextPropagation.InjectContext(activity, message.Headers);

                return activity;
            }

            public Activity? StartGetMessage(RabbitMqEndpoint endpoint, string queueName)
            {
                Preconditions.CheckNotNull(endpoint, nameof(endpoint));
                Preconditions.CheckNotNull(queueName, nameof(queueName));

                var activity = _activitySource.StartActivity("Get", endpoint, ActivityKind.Consumer);
                if (activity is null)
                    return activity;

                SetQueueNameTags(activity, queueName);

                return activity;
            }

            public Activity? StartCompleteMessage(RabbitMqEndpoint endpoint, ReceivedRabbitMqMessage message,
                ConsumeResult consumeResult)
            {
                Preconditions.CheckNotNull(endpoint, nameof(endpoint));
                Preconditions.CheckNotNull(message, nameof(message));

                var activity = _activitySource.StartActivity("Complete", endpoint, ActivityKind.Consumer);
                if (activity is null)
                    return activity;

                ActivityContextPropagation.ExtractContext(activity, message.Headers);

                activity.SetTag("amqp.message.consume_result", consumeResult.ToString());
                activity.SetTag("amqp.message.delivery_tag", message.DeliveryTag);

                return activity;
            }

            public Activity? StartConsume(RabbitMqEndpoint endpoint, ReceivedRabbitMqMessage message)
            {
                var activity = _activitySource.StartActivity("Consume", endpoint, ActivityKind.Consumer);

                if (activity is not {IsAllDataRequested: true})
                    return activity;

                _activitySource.Events.MessageGot(activity, message);

                return activity;
            }
        }
    }
}