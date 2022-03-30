using System;
using Byndyusoft.Messaging.Abstractions;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class QueueMessageFactory
    {
        public static QueueMessage CreateRetryMessage(ConsumedQueueMessage consumedMessage, string retryQueueName)
        {
            var headers = new QueueMessageHeaders(consumedMessage.Headers);
            headers.SetRetryCount(consumedMessage.RetryCount);

            return new QueueMessage
            {
                Content = RabbitMessageContent.Create(consumedMessage.Content),
                Properties = consumedMessage.Properties,
                Mandatory = false,
                Persistent = consumedMessage.Persistent,
                Headers = headers,
                RoutingKey = retryQueueName
            };
        }

        public static QueueMessage CreateErrorMessage(ConsumedQueueMessage consumedMessage, string errorQueueName,
            Exception? exception = null)
        {
            var headers = new QueueMessageHeaders(consumedMessage.Headers);
            headers.SetException(exception);
            headers.RemovedRetryData();

            return new QueueMessage
            {
                Content = RabbitMessageContent.Create(consumedMessage.Content),
                Properties = consumedMessage.Properties,
                Mandatory = false,
                Persistent = consumedMessage.Persistent,
                Headers = headers,
                RoutingKey = errorQueueName
            };
        }
    }
}