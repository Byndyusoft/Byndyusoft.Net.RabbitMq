using System;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions.Topology
{
    public class QueueNamingConventions
    {
        private CreateQueueNameDelegate _createQueueName = (exchangeName, routingKey, application) =>
            $"{exchangeName}::{application}::{routingKey}".ToLowerInvariant();

        private Func<string, string> _errorQueueName = queue => $"{queue}.error";

        private Func<string, string> _retryQueueName = queue => $"{queue}.retry";

        public Func<string, string> ErrorQueueName
        {
            get => _errorQueueName;
            set => _errorQueueName = Preconditions.CheckNotNull(value, nameof(ErrorQueueName));
        }

        public Func<string, string> RetryQueueName
        {
            get => _retryQueueName;
            set => _retryQueueName = Preconditions.CheckNotNull(value, nameof(RetryQueueName));
        }

        public CreateQueueNameDelegate QueueName
        {
            get => _createQueueName;
            set => _createQueueName = Preconditions.CheckNotNull(value, nameof(QueueName));
        }
    }
}