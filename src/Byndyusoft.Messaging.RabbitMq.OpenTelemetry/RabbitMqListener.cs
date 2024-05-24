using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Byndyusoft.Logging;
using Byndyusoft.Logging.Extensions;
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

        private StructuredActivityEventItem[]? BuildMessagePublishingEventItems(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is not RabbitMqMessage message)
                return null;

            // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
            // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
            var eventItems = new StructuredActivityEventItem[]
            {
                new("messaging.operation", "publish"),
                new("messaging.system", "rabbitmq"),
                new("messaging.message.client_id", message.Properties.AppId),
                new("messaging.message.id", message.Properties.MessageId),
                new("messaging.message.conversation_id", message.Properties.CorrelationId),
                new("messaging.message.body.size", message.Content.Headers.ContentLength),
                new("messaging.rabbitmq.message.properties", JsonSerializer.Serialize(message.Properties, options)),
                new("messaging.rabbitmq.destination.exchange", message.Exchange ?? string.Empty),
                new("messaging.rabbitmq.destination.routing_key", message.RoutingKey),
                new("messaging.rabbitmq.message.mandatory", message.Mandatory)
            };

            return eventItems;
        }

        private void OnMessageReturned(Activity? activity, object? payload)
        {
            var eventItems = BuildMessageReturnedEventItems(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.returned");
        }

        private StructuredActivityEventItem[]? BuildMessageReturnedEventItems(object? payload, RabbitMqDiagnosticsOptions options)
        {
            if (payload is not ReturnedRabbitMqMessage message)
                return null;

            // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
            // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
            var eventItems = new StructuredActivityEventItem[]
            {
                new("messaging.operation", "return"),
                new("messaging.system", "rabbitmq"),
                new("messaging.message.client_id", message.Properties.AppId),
                new("messaging.message.id", message.Properties.MessageId),
                new("messaging.message.conversation_id", message.Properties.CorrelationId),
                new("messaging.message.body.size", message.Content.Headers.ContentLength),
                new("messaging.rabbitmq.message.properties", JsonSerializer.Serialize(message.Properties, options)),
                new("messaging.rabbitmq.destination.exchange", message.Exchange ?? string.Empty),
                new("messaging.rabbitmq.destination.routing_key", message.RoutingKey),
                new("messaging.rabbitmq.return.reason", message.ReturnReason)
            };

            return eventItems;
        }

        private void OnMessageGot(Activity? activity, object? payload)
        {
            EnrichLogsWithQueueInfo(payload);

            var eventItems = BuildMessageConsumingEventItems(payload);
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

        private StructuredActivityEventItem? GetQueueInfo(object? payload)
        {
            if (payload is not ReceivedRabbitMqMessage message)
                return null;

            return new StructuredActivityEventItem("messaging.rabbitmq.destination.queue", message.Queue);
        }

        private StructuredActivityEventItem[]? BuildMessageConsumingEventItems(object? payload)
        {
            if (payload is null)
            {
                return new StructuredActivityEventItem[]
                {
                    new("messaging.message", null)
                };
            }

            if (payload is not ReceivedRabbitMqMessage message)
                return null;

            return EnumerateMessageConsumingEventItems(message).ToArray();
        }

        private IEnumerable<StructuredActivityEventItem> EnumerateMessageConsumingEventItems(
            ReceivedRabbitMqMessage message)
        {
            if (_options.LogContentType == LogContentType.RawString)
                yield return new StructuredActivityEventItem(
                    "messaging.message.body",
                    message.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            // https://opentelemetry.io/docs/specs/semconv/attributes-registry/messaging/
            // https://opentelemetry.io/docs/specs/semconv/messaging/rabbitmq/
            yield return new StructuredActivityEventItem("messaging.operation", "receive");
            yield return new StructuredActivityEventItem("messaging.system", "rabbitmq");
            yield return new StructuredActivityEventItem("messaging.message.client_id", message.Properties.AppId);
            yield return new StructuredActivityEventItem("messaging.message.id", message.Properties.MessageId);
            yield return new StructuredActivityEventItem("messaging.message.conversation_id",
                message.Properties.CorrelationId);
            yield return new StructuredActivityEventItem("messaging.message.body.size",
                message.Content.Headers.ContentLength);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.message.properties",
                JsonSerializer.Serialize(message.Properties, _options.DiagnosticsOptions));
            yield return new StructuredActivityEventItem("messaging.rabbitmq.destination.exchange",
                message.Exchange ?? string.Empty);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.destination.routing_key",
                message.RoutingKey);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.message.delivery_tag",
                message.DeliveryTag);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.message.redelivered", message.Redelivered);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.message.consumer_tag",
                message.ConsumerTag);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.message.retry_count", message.RetryCount);
            yield return new StructuredActivityEventItem("messaging.rabbitmq.destination.queue", message.Queue);
        }

        private void OnMessageReplied(Activity? activity, object? payload)
        {
            EnrichLogsWithQueueInfo(payload);

            var eventItems = BuildMessageConsumingEventItems(payload);
            Log(activity, eventItems, "message.replied");
        }

        private void OnMessageConsumed(Activity? activity, object? payload)
        {
            var eventItems = BuildMessageConsumedEventItems(payload);
            Log(activity, eventItems, "message.consumed");
        }

        private StructuredActivityEventItem[]? BuildMessageConsumedEventItems(object? payload)
        {
            if (payload is not ConsumeResult result)
                return null;

            var eventItems = new StructuredActivityEventItem[]
            {
                new("result", result.GetDescription())
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

        private StructuredActivityEventItem[]? BuildUnhandledExceptionEventItems(object? payload)
        {
            if (payload is not Exception exception)
                return null;

            var eventItems = new StructuredActivityEventItem[]
            {
                new("exception.type", exception.GetType().FullName),
                new("exception.message", exception.Message),
                new("exception.stacktrace", exception.ToString())
            };

            return eventItems;
        }

        private void OnMessageModelRead(Activity? activity, object? payload)
        {
            EnrichWithModelTelemetryItems(activity, payload);
            LogModelRead(activity, payload);
        }

        private void EnrichWithModelTelemetryItems(Activity? activity, object? payload)
        {
            if (_options.EnrichLogsWithParams == false &&
                (activity is null || _options.TagRequestParamsInTrace == false))
                return;

            var telemetryItems =
                ObjectTelemetryItemsCollector.Collect("rabbitMqModel", payload, "messaging.message.params.");

            if (_options.EnrichLogsWithParams)
                LogPropertyDataAccessor.AddTelemetryItems(telemetryItems);

            if (activity is not null && _options.TagRequestParamsInTrace)
                ActivityTagEnricher.Enrich(activity, telemetryItems);
        }

        private void LogModelRead(Activity? activity, object? payload)
        {
            if (_options.LogContentType != LogContentType.ReadModel)
                return;

            var eventItems = new StructuredActivityEventItem[]
            {
                new("messaging.message.body",
                    JsonSerializer.Serialize(payload, _options.DiagnosticsOptions))
            };
            Log(activity, eventItems, "message.model.read");
        }

        private void Log(Activity? activity, StructuredActivityEventItem[]? eventItems, string eventName)
        {
            if (eventItems is null)
                return;

            if (activity is not null && _options.LogEventsInTrace)
                LogEventInTrace(activity, eventItems, eventName);

            if (_options.LogEventsInLogs)
                LogEventInLog(eventItems, eventName);
        }

        private void LogEventInTrace(Activity activity, StructuredActivityEventItem[] eventItems, string eventName)
        {
            var tags = new ActivityTagsCollection();
            foreach (var eventItem in eventItems) 
                tags.Add(eventItem.Name, eventItem.Value);

            var activityEvent = new ActivityEvent(eventName, tags: tags);
            activity.AddEvent(activityEvent);
        }

        private void LogEventInLog(StructuredActivityEventItem[] eventItems, string eventName)
        {
            _logger.LogStructuredActivityEvent(eventName, eventItems);
        }
    }
}