using System;
using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;

namespace Byndyusoft.Messaging.RabbitMq.Messages
{
    internal static class RabbitMqMessageFactory
    {
        public static RabbitMqMessage CreateRetryMessage(ReceivedRabbitMqMessage consumedMessage, string retryQueueName)
        {
            var headers = new RabbitMqMessageHeaders(consumedMessage.Headers);
            
            var activity = Activity.Current;
            ActivityContextPropagation.InjectContext(activity, headers);
            
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

            var activity = Activity.Current;
            ActivityContextPropagation.InjectContext(activity, headers);

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