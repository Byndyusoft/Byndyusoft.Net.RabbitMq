using System;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Topology
{
    public class QueueNamingConventions
    {
        private CreateQueueNameDelegate _createQueueName = (exchangeName, routingKey, application) =>
            $"{exchangeName}::{application}::{routingKey}".ToLowerInvariant();

        private CreateErrorQueueNameDelegate _errorQueueName = queue => $"{queue}.error";

        private CreateRetryQueueNameDelegate _retryQueueName = queue => $"{queue}.retry";

        private CreateRpcReplyQueueNameDelegate _rpcReplyQueueName = application => $"{application}.{Guid.NewGuid()}.rpc.reply";

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

        public CreateRpcReplyQueueNameDelegate RpcReplyQueueName
        {
            get => _rpcReplyQueueName;
            set => _rpcReplyQueueName = Preconditions.CheckNotNull(value, nameof(RpcReplyQueueName));
        }
    }
}