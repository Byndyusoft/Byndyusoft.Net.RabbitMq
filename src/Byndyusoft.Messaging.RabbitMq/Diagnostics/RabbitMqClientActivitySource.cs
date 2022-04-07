using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Serialization;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public static readonly string Name = typeof(RabbitMqClientActivitySource).Assembly.GetName().Name;

        private static readonly string? Version = typeof(RabbitMqClientActivitySource)
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        private readonly RabbitMqDiagnosticsOptions _options;

        private readonly ActivitySource _source;

        public RabbitMqClientActivitySource(RabbitMqDiagnosticsOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            _source = new ActivitySource(Name, Version);
            _options = options;

            Events = new RabbitMqClientActivitySourceEvents(this);
            Activities = new RabbitMqClientActivitySourceActivities(this);
        }

        public RabbitMqClientActivitySourceActivities Activities { get; }

        public RabbitMqClientActivitySourceEvents Events { get; }

        private Activity? StartActivity(string name, RabbitMqEndpoint endpoint, ActivityKind kind)
        {
            Preconditions.CheckNotNull(name, nameof(name));
            Preconditions.CheckNotNull(endpoint, nameof(endpoint));

            var activity = _source.StartActivity(name, kind);
            if (activity is not {IsAllDataRequested: true})
                return activity;

            SetConnectionTags(activity, endpoint);

            return activity;
        }

        private static void StopActivity(Activity? activity)
        {
            if (activity is null)
                return;

            activity.SetStatus(ActivityStatusCode.Ok);
            activity.SetTag("otel.status_code", "OK");
            activity.Dispose();
        }

        private static void SetException(Activity? activity, Exception exception, bool escaped = true)
        {
            Preconditions.CheckNotNull(exception, nameof(exception));

            if (activity is null)
                return;

            activity.SetStatus(ActivityStatusCode.Error, exception.Message);

            var tags = new ActivityTagsCollection
            {
                {"exception.type", exception.GetType().FullName},
                {"exception.message", exception.Message},
                {"exception.stacktrace", exception.ToString()},
                {"exception.escaped", escaped}
            };
            var activityEvent = new ActivityEvent("exception", tags: tags);
            activity.AddEvent(activityEvent);
            activity.SetTag("error", "true");
            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", exception.Message);
            activity.Dispose();
        }

        private void SetMessageTags(Activity activity, RabbitMqMessage message, int? index = null)
        {
            var prefix = index is null
                ? "amqp.message"
                : $"amqp.message[{index}]";

            activity.SetTag($"{prefix}.exchange", message.Exchange ?? string.Empty);
            activity.SetTag($"{prefix}.routing_key", message.RoutingKey);
            activity.SetTag($"{prefix}.mandatory", message.Mandatory);
            activity.SetTag($"{prefix}.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            activity.SetTag($"{prefix}.properties", JsonSerializer.Serialize(message.Properties, _options));
        }

        private static void SetConnectionTags(Activity activity, RabbitMqEndpoint endpoint)
        {
            activity.SetTag("net.transport", "amqp");
            activity.SetTag("net.peer.name", endpoint.Host);

            if (endpoint.Port is not null)
                activity.SetTag("net.peer.port", endpoint.Port);
        }

        private static void SetQueueNameTags(Activity activity, string queueName)
        {
            activity.SetTag("amqp.queue_name", queueName);
        }

        public Task ExecuteAsync(Activity? activity, Func<Task> action)
        {
            return ExecuteAsync(activity,
                async () =>
                {
                    await action();
                    return 0;
                });
        }

        public async Task<T> ExecuteAsync<T>(Activity? activity, Func<Task<T>> action)
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