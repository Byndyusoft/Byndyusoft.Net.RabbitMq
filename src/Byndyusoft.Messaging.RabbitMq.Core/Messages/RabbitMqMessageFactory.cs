using System;

namespace Byndyusoft.Messaging.RabbitMq.Messages
{
    internal static class RabbitMqMessageFactory
    {
        public static RabbitMqMessage CreateRetryMessage(ReceivedRabbitMqMessage consumedMessage, string retryQueueName)
        {
            var headers = new RabbitMqMessageHeaders(consumedMessage.Headers);

            return new RabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(consumedMessage.Content),
                Properties = consumedMessage.Properties,
                Mandatory = true,
                Persistent = consumedMessage.Persistent,
                Headers = headers,
                RoutingKey = retryQueueName
            };
        }

        public static RabbitMqMessage CreateErrorMessage(ReceivedRabbitMqMessage consumedMessage,
            string errorQueueName,
            Exception? exception = null)
        {
            var headers = new RabbitMqMessageHeaders(consumedMessage.Headers);
            headers.SetException(exception);
            headers.RemoveRetryData();

            return new RabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(consumedMessage.Content),
                Properties = consumedMessage.Properties,
                Mandatory = true,
                Persistent = consumedMessage.Persistent,
                Headers = headers,
                RoutingKey = errorQueueName
            };
        }
    }
}