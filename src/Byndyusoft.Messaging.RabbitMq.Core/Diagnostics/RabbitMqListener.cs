using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Base;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Builders;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;
using Byndyusoft.Messaging.RabbitMq.Settings;
using Microsoft.Extensions.Logging;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
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

        public override bool SupportsNullActivity => true;

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
        }

        private bool IsProcessingNeeded(Activity? activity)
        {
            if (_options.LogEventsInLogs)
                return true;

            return activity is not null && _options.LogEventsInTrace;
        }

        private void OnMessagePublishing(Activity? activity, object? payload)
        {
            var eventItems = EventItemBuilder.BuildFromMessagePublishing(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.publishing");
        }

        private void OnMessageReturned(Activity? activity, object? payload)
        {
            var eventItems = EventItemBuilder.BuildFromMessageReturned(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.returned");
        }

        private void OnMessageGot(Activity? activity, object? payload)
        {
            var eventItems = EventItemBuilder.BuildFromMessageGot(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.got");
        }

        private void OnMessageReplied(Activity? activity, object? payload)
        {
            var eventItems = EventItemBuilder.BuildFromMessageReplied(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.replied");
        }

        private void OnMessageConsumed(Activity? activity, object? payload)
        {
            var eventItems = EventItemBuilder.BuildFromMessageConsumed(payload, _options.DiagnosticsOptions);
            Log(activity, eventItems, "message.consumed");
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