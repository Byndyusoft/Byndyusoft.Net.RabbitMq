using System;

namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueServiceOptions
    {
        public Func<string, string> ErrorQueueName = message => $"{message}.error";

        public Func<string, string> RetryQueueName = message => $"{message}.retry";
        public string ConnectionString { get; set; } = default!;

        public string? ApplicationName { get; set; }
    }
}