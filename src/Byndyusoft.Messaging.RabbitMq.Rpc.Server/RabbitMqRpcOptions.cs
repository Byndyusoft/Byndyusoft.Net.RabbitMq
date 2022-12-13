using System;
using System.Collections.Generic;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Rpc
{
    public class RabbitMqRpcOptions
    {
        private Dictionary<string, string> _queueNames = new();
        private Func<string, QueueOptions> _queueOption = _ => QueueOptions.Default;

        public Dictionary<string, string> QueueNames
        {
            get => _queueNames;
            set => _queueNames = Preconditions.CheckNotNull(value, nameof(QueueNames));
        }

        public Func<string, QueueOptions> QueueOption
        {
            get => _queueOption;
            set => _queueOption = Preconditions.CheckNotNull(value, nameof(QueueOption));
        }

        internal QueueOptions GetQueueOptions(string queueName)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            return QueueOption(queueName) ?? QueueOptions.Default;
        }

        internal string GetQueueName(string queueNameOrKey)
        {
            Preconditions.CheckNotNull(queueNameOrKey, nameof(queueNameOrKey));
            return QueueNames.GetValueOrDefault(queueNameOrKey) ?? queueNameOrKey;
        }
    }
}