using System;
using System.Reflection;

namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueServiceOptions
    {
        public Func<string, string> ErrorQueueName = message => $"{message}.error";

        public Func<(string exchange, string routingKey, string application), string> QueueName = x =>
            $"{x.exchange}::{x.application}::{x.routingKey}".ToLowerInvariant();

        public Func<string, string> RetryQueueName = message => $"{message}.retry";

        public string ConnectionString { get; set; } = default!;

        public string ApplicationName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name;
    }
}