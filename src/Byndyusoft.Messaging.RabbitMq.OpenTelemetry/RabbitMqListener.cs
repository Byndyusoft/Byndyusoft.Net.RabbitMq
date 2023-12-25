using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;
using Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Base;
using Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Serialization;
using Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Settings;
using Byndyusoft.Telemetry;
using Byndyusoft.Telemetry.Logging;
using Byndyusoft.Telemetry.OpenTelemetry;
using Microsoft.Extensions.Logging;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry
{
    public class RabbitMqListener : ListenerHandler
    {
        private const string ExchangeDescription = "Exchange";
        private const string RoutingKeyDescription = "RoutingKey";
        private const string QueueDescription = "Queue";
        private const string ContentDescription = "RoutingKey";
        private const string PropertiesDescription = "RoutingKey";

        private readonly ILogger<RabbitMqListener> _logger;
        private readonly RabbitMqTracingOptions _options;

        public RabbitMqListener(
            ILogger<RabbitMqListener> logger,
            RabbitMqTracingOptions options)
            : base(DiagnosticNames.RabbitMq)
        {
            _logger = logger;
            _options = options;
        }

        public override void OnEventWritten(string name, object? payload)
        {
            var activity = Activity.Current;
            if (IsProcessingNeeded(activity) == false)
                return;

            if (name.Equals(EventNames.MessagePublishing))
                OnMessagePublishing(activity, payload);
            else if (name.Equals(EventNames.MessageReturned))
                OnMessageReturned(activity, payload);
            else if (name.Equals(EventNames.MessageGot))
                OnMessageGot(activity, payload);
            else if (name.Equals(EventNames.MessageReplied))
                OnMessageReplied(activity, payload);
            else if (name.Equals(EventNames.MessageConsumed))
                OnMessageConsumed(activity, payload);
            else if (name.Equals(EventNames.UnhandledException))
                OnUnhandledException(activity, payload);
            else if (name.Equals(EventNames.MessageModelRead))
                OnMessageModelRead(activity, payload);
        }

        private bool IsProcessingNeeded(Activity? activity)
        {
            if (_options.LogEventsInLogs || _options.EnrichLogsWithParams || _options.EnrichLogsWithQueueInfo)
                return true;

            return activity is not null && (_options.LogEventsInTrace || _options.TagRequestParamsInTrace);
        }

        private void OnMessagePublishing(Activity? activity, object? payload)
        {
            var eventItems = BuildMessagePublishingEventItems(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.publishing");
        }

        private EventItem[]? BuildMessagePublishingEventItems(object? payload, RabbitMqDiagnosticsOptions options)
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

        private void OnMessageReturned(Activity? activity, object? payload)
        {
            var eventItems = BuildMessageReturnedEventItems(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.returned");
        }

        private EventItem[]? BuildMessageReturnedEventItems(object? payload, RabbitMqDiagnosticsOptions options)
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

        private void OnMessageGot(Activity? activity, object? payload)
        {
            EnrichLogsWithQueueInfo(payload);

            var eventItems = BuildMessageConsumingEventItems(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.got");
        }

        private void EnrichLogsWithQueueInfo(object? payload)
        {
            if (_options.EnrichLogsWithQueueInfo == false)
                return;

            var queueInfo = GetQueueInfo(payload);
            if (queueInfo is not null)
                LogPropertyDataAccessor.AddTelemetryItem(queueInfo.Name, queueInfo.Value);
        }

        private EventItem? GetQueueInfo(object? payload)
        {
            if (payload is not ReceivedRabbitMqMessage message)
                return null;

            return new EventItem("amqp.message.queue", message.Queue, QueueDescription);
        }

        private EventItem[]? BuildMessageConsumingEventItems(object? payload, RabbitMqDiagnosticsOptions options)
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

        private void OnMessageReplied(Activity? activity, object? payload)
        {
            EnrichLogsWithQueueInfo(payload);

            var eventItems = BuildMessageConsumingEventItems(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.replied");
        }

        private void OnMessageConsumed(Activity? activity, object? payload)
        {
            var eventItems = BuildMessageConsumedEventItems(payload);
            Log(activity, eventItems, "message.consumed");
        }

        private EventItem[]? BuildMessageConsumedEventItems(object? payload)
        {
            if (payload is not ConsumeResult result)
                return null;

            var eventItems = new EventItem[]
            {
                new("result", result.GetDescription(), "Result")
            };

            return eventItems;
        }

        private void OnUnhandledException(Activity? activity, object? payload)
        {
            if (_options.RecordExceptions == false)
                return;

            var eventItems = BuildUnhandledExceptionEventItems(payload);
            Log(activity, eventItems, "exception");
        }

        private EventItem[]? BuildUnhandledExceptionEventItems(object? payload)
        {
            if (payload is not Exception exception)
                return null;

            var eventItems = new EventItem[]
            {
                new("exception.type", exception.GetType().FullName, "Type"),
                new("exception.message", exception.Message, "Message"),
                new("exception.stacktrace", exception.ToString(), "StackTrace")
            };

            return eventItems;
        }

        private void OnMessageModelRead(Activity? activity, object? payload)
        {
            if (_options.EnrichLogsWithParams == false &&
                (activity is null || _options.TagRequestParamsInTrace == false))
                return;

            var telemetryItems =
                ObjectTelemetryItemsCollector.Collect("rabbitMqModel", payload, "amqp.message.params.");

            if (_options.EnrichLogsWithParams)
                LogPropertyDataAccessor.AddTelemetryItems(telemetryItems);

            if (activity is not null && _options.TagRequestParamsInTrace)
                ActivityTagEnricher.Enrich(activity, telemetryItems);
        }

        private void Log(Activity? activity, EventItem[]? eventItems, string eventName)
        {
            if (eventItems is null)
                return;

            if (activity is not null && _options.LogEventsInTrace)
                LogEventInTrace(activity, eventItems, eventName);

            if (_options.LogEventsInLogs)
                LogEventInLog(eventItems, eventName);
        }

        private void LogEventInTrace(Activity activity, EventItem[] eventItems, string eventName)
        {
            var tags = new ActivityTagsCollection();
            foreach (var eventItem in eventItems) 
                tags.Add(eventItem.Name, eventItem.Value);

            var activityEvent = new ActivityEvent(eventName, tags: tags);
            activity.AddEvent(activityEvent);
        }

        private void LogEventInLog(EventItem[] eventItems, string logPrefix)
        {
            var messageBuilder = new StringBuilder($"{logPrefix}: ");
            var parameters = new List<object?>();
            foreach (var eventItem in eventItems)
            {
                var itemName = eventItem.Name.Replace('.', '_');
                messageBuilder.Append($"{eventItem.Description} = {{{itemName}}}; ");
                parameters.Add(eventItem.Value);
            }

            _logger.LogInformation(messageBuilder.ToString(), parameters.ToArray());
        }
    }
}