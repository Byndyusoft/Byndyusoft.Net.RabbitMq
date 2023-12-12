using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Base;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Builders;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public class RabbitMqActivityListener : ListenerHandler
    {
        private readonly RabbitMqClientCoreOptions _options;

        public RabbitMqActivityListener(
            RabbitMqClientCoreOptions options)
            : base(DiagnosticNames.RabbitMq)
        {
            _options = options;
        }

        public override bool SupportsNullActivity => true;

        public override void OnEventWritten(string name, object? payload)
        {
            if (name.Equals(EventNames.MessagePublishing))
                OnMessagePublishing(payload);
            else if (name.Equals(EventNames.MessageReturned))
                OnMessageReturned(payload);
            else if (name.Equals(EventNames.MessageGot))
                OnMessageGot(payload);
            else if (name.Equals(EventNames.MessageReplied))
                OnMessageReplied(payload);
            else if (name.Equals(EventNames.MessageConsumed))
                OnMessageConsumed(payload);
        }

        private void OnMessagePublishing(object? payload)
        {
            var activity = Activity.Current;
            if (activity is null)
                return;

            var eventItems = EventItemBuilder.BuildFromMessagePublishing(payload, _options.DiagnosticsOptions);
            LogEvent(activity, eventItems, "message.publishing");
        }

        private void OnMessageReturned(object? payload)
        {
            var activity = Activity.Current;
            if (activity is null)
                return;

            var eventItems = EventItemBuilder.BuildFromMessageReturned(payload, _options.DiagnosticsOptions);
            LogEvent(activity, eventItems, "message.returned");
        }

        private void OnMessageGot(object? payload)
        {
            var activity = Activity.Current;
            if (activity is null)
                return;

            var eventItems = EventItemBuilder.BuildFromMessageGot(payload, _options.DiagnosticsOptions);
            LogEvent(activity, eventItems, "message.got");
        }

        private void OnMessageReplied(object? payload)
        {
            var activity = Activity.Current;
            if (activity is null)
                return;

            var eventItems = EventItemBuilder.BuildFromMessageReplied(payload, _options.DiagnosticsOptions);
            LogEvent(activity, eventItems, "message.replied");
        }

        private void OnMessageConsumed(object? payload)
        {
            var activity = Activity.Current;
            if (activity is null)
                return;

            var eventItems = EventItemBuilder.BuildFromMessageConsumed(payload, _options.DiagnosticsOptions);
            LogEvent(activity, eventItems, "message.consumed");
        }

        private void LogEvent(Activity activity, EventItem[]? eventItems, string eventName)
        {
            if (eventItems is null)
                return;

            var tags = new ActivityTagsCollection();
            foreach (var eventItem in eventItems) 
                tags.Add(eventItem.Name, eventItem.Value);

            var activityEvent = new ActivityEvent(eventName, tags: tags);
            activity.AddEvent(activityEvent);
        }
    }
}