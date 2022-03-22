using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Propagation;
using Byndyusoft.Messaging.Serialization;
using EasyNetQ;

namespace Byndyusoft.Messaging.Core
{
    public class QueueServiceActivitySource
    {
        public static readonly string Name = typeof(QueueServiceActivitySource).Assembly.GetName().Name;

        private static readonly ActivitySource Source;

        static QueueServiceActivitySource()
        {
            var assembly = typeof(QueueServiceActivitySource).Assembly;
            var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0";
            Source = new ActivitySource(Name, version);
        }

        //internal static bool IsEnabled => Source.HasListeners();

        internal static Activity? StartPublish(ConnectionConfiguration connectionConfiguration, QueueMessage message)
        {
            var activity = Source.StartActivity(nameof(QueueService.PublishAsync), ActivityKind.Client);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, connectionConfiguration);
            SetMessageTags(activity, message);
            ActivityContextPropagation.SetContext(activity, message);

            return activity;
        }

        internal static Activity? StartBatchPublish(ConnectionConfiguration connectionConfiguration,
            IEnumerable<QueueMessage> messages)
        {
            var activity = Source.StartActivity(nameof(QueueService.PublishBatchAsync), ActivityKind.Client);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, connectionConfiguration);

            var index = 0;
            foreach (var message in messages)
            {
                SetMessageTags(activity, message, index++);
                ActivityContextPropagation.SetContext(activity, message);
            }

            return activity;
        }

        internal static Activity? StartGet(ConnectionConfiguration connectionConfiguration, string queueName)
        {
            var activity = Source.StartActivity(nameof(QueueService.GetAsync), ActivityKind.Client);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, connectionConfiguration);
            SetQueueNameTags(activity, queueName);

            return activity;
        }

        internal static Activity? StartAck(ConnectionConfiguration connectionConfiguration,
            ConsumedQueueMessage message)
        {
            var activity = Source.StartActivity(nameof(QueueService.AckAsync), ActivityKind.Client);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, connectionConfiguration);
            activity.SetTag("message_bus.message.delivery_tag", message.DeliveryTag);

            return activity;
        }

        internal static Activity? StartReject(ConnectionConfiguration connectionConfiguration,
            ConsumedQueueMessage message, bool requeue)
        {
            var activity = Source.StartActivity(nameof(QueueService.RejectAsync), ActivityKind.Client);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, connectionConfiguration);
            activity.SetTag("message_bus.message.delivery_tag", message.DeliveryTag);
            activity.SetTag("message_bus.message.requeue", requeue);

            return activity;
        }

        internal static Activity? StartConsume(ConnectionConfiguration connectionConfiguration,
            ConsumedQueueMessage message)
        {
            var activity = Source.StartActivity(nameof(IQueueServiceHandler.Consume), ActivityKind.Server);

            if (activity is not {IsAllDataRequested: true})
                return activity;

            ActivityContextPropagation.ExtractContext(activity, message);

            SetConnectionTags(activity, connectionConfiguration);

            var tags = GetConsumedMessageEventTags(message);
            var activityEvent = new ActivityEvent("message.got", tags: tags);
            activity.AddEvent(activityEvent);

            return activity;
        }

        internal static void MessageConsumed(Activity? activity, ConsumedQueueMessage _, ConsumeResult result)
        {
            if (activity is null)
                return;

            var tags = new ActivityTagsCollection {{"result", result.ToString()}};
            var activityEvent = new ActivityEvent("message.consumed", tags: tags);
            activity.AddEvent(activityEvent);
        }

        internal static void StopActivity(Activity? activity)
        {
            if (activity is null)
                return;

            activity.SetTag("otel.status_code", "OK");
            activity.Dispose();
        }

        internal static void SetException(Activity? activity, Exception ex, bool escaped = true)
        {
            if (activity is null)
                return;

            var tags = new ActivityTagsCollection
            {
                {"exception.type", ex.GetType().FullName},
                {"exception.message", ex.Message},
                {"exception.stacktrace", ex.ToString()},
                {"exception.escaped", escaped}
            };
            var activityEvent = new ActivityEvent("exception", tags: tags);
            activity.AddEvent(activityEvent);
            activity.SetTag("error", "true");
            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", ex.Message);
            activity.Dispose();
        }

        internal static void MessageGot(Activity? activity, ConsumedQueueMessage? message)
        {
            if (activity is null)
                return;

            ActivityContextPropagation.ExtractContext(activity, message);

            var tags = GetConsumedMessageEventTags(message);
            var activityEvent = new ActivityEvent("message.got", tags: tags);
            activity.AddEvent(activityEvent);
        }

        private static void SetMessageTags(Activity activity, QueueMessage message, int? index = null)
        {
            var prefix = index is null
                ? "message_bus.message"
                : $"message_bus.message[{index}]";

            activity.SetTag($"{prefix}.exchange", message.Exchange ?? string.Empty);
            activity.SetTag($"{prefix}.routing_key", message.RoutingKey);
            activity.SetTag($"{prefix}.mandatory", message.Mandatory);
            activity.SetTag($"{prefix}.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            activity.SetTag($"{prefix}.properties", JsonConvert.Serialize(message.Properties));
        }

        private static void SetConnectionTags(Activity activity, ConnectionConfiguration connectionConfiguration)
        {
            activity.SetTag("message_bus.transport", "amqp");

            var host = connectionConfiguration.Hosts.FirstOrDefault();
            if (host != null)
            {
                activity.SetTag("message_bus.peer.name", host.Host);
                if (host.Port != ConnectionConfiguration.DefaultPort)
                    activity.SetTag("message_bus.peer.port", host.Port);
            }
        }

        private static void SetQueueNameTags(Activity activity, string queueName)
        {
            activity.SetTag("message_bus.queue_name", queueName);
        }

        private static ActivityTagsCollection GetConsumedMessageEventTags(ConsumedQueueMessage? message)
        {
            var tags = new ActivityTagsCollection();

            if (message is null)
            {
                tags.Add("message", "null");
            }
            else
            {
                tags.Add("message.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                tags.Add("message.exchange", message.Exchange);
                tags.Add("message.queue", message.Queue);
                tags.Add("message.routing_key", message.RoutingKey);
                tags.Add("message.delivery_tag", message.DeliveryTag);
                tags.Add("message.redelivered", message.Redelivered);
                tags.Add("message.consumer_tag", message.ConsumerTag);
                tags.Add("message.properties", JsonConvert.Serialize(message.Properties));
            }

            return tags;
        }

        public static async Task<T> ExecuteAsync<T>(Activity? activity, Func<Task<T>> action)
        {
            try
            {
                var result = await action().ConfigureAwait(false);
                StopActivity(activity);
                return result;
            }
            catch (Exception exception)
            {
                SetException(activity, exception);
                throw;
            }
        }
    }
}