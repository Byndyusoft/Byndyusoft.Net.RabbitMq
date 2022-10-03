using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Topology
{
    public class QueueNamingConventions
    {
        private CreateQueueNameDelegate _createQueueName = (exchangeName, routingKey, application) =>
            $"{exchangeName}::{application}::{routingKey}".ToLowerInvariant();

        private CreateErrorQueueNameDelegate _errorQueueName = queue => $"{queue}.error";

        private CreateRetryQueueNameDelegate _retryQueueName = queue => $"{queue}.retry";

        public CreateErrorQueueNameDelegate ErrorQueueName
        {
            get => _errorQueueName;
            set => _errorQueueName = Preconditions.CheckNotNull(value, nameof(ErrorQueueName));
        }

        public CreateRetryQueueNameDelegate RetryQueueName
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