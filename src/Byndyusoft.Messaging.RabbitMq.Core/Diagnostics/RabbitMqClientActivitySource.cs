using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public static readonly string Name = typeof(RabbitMqClientActivitySource).Assembly.GetName().Name;

        private static readonly string? Version = typeof(RabbitMqClientActivitySource)
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        private readonly ActivitySource _source;

        public RabbitMqClientActivitySource()
        {
            _source = new ActivitySource(Name, Version);

            Activities = new RabbitMqClientActivitySourceActivities(this);
        }

        public RabbitMqClientActivitySourceActivities Activities { get; }

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
            activity.Dispose();
        }

        private static void SetException(Activity? activity, Exception exception)
        {
            Preconditions.CheckNotNull(exception, nameof(exception));
            RabbitMqClientEvents.OnUnhandledException(exception);

            if (activity is null)
                return;

            activity.SetTag("error", "true");
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.Dispose();
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