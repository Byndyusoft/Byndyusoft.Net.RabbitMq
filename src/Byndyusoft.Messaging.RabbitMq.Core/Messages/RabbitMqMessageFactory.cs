using System;
using System.Diagnostics;
using System.Net.Http;
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

        public static RabbitMqMessage CreateRpcResponseMessage(ReceivedRabbitMqMessage requestMessage,
            RpcResult rpcResult)
        {
            return rpcResult switch
            {
                RpcSuccessResult successResult => CreateRpcSuccessResponseMessage(requestMessage, successResult),
                RpcErrorResult errorResult => CreateRpcErrorResponseMessage(requestMessage, errorResult),
                _ => throw new ArgumentOutOfRangeException(nameof(rpcResult))
            };
        }

        public static RabbitMqMessage CreateRpcSuccessResponseMessage(ReceivedRabbitMqMessage requestMessage,
            RpcSuccessResult response)
        {
            var replyTo = requestMessage.Properties.ReplyTo!;
            var correlationId = requestMessage.Properties.CorrelationId!;

            var headers = new RabbitMqMessageHeaders();
            ActivityContextPropagation.InjectContext(Activity.Current, headers);

            return new RabbitMqMessage
            {
                Content = response.Response,
                Properties = new RabbitMqMessageProperties
                {
                    CorrelationId = correlationId
                },
                Mandatory = false,
                Persistent = requestMessage.Persistent,
                Headers = headers,
                RoutingKey = replyTo,
                Exchange = null,
            };
        }

        public static RabbitMqMessage CreateRpcErrorResponseMessage(ReceivedRabbitMqMessage requestMessage,
            RpcErrorResult response)
        {
            var replyTo = requestMessage.Properties.ReplyTo!;
            var correlationId = requestMessage.Properties.CorrelationId!;

            var headers = new RabbitMqMessageHeaders();
            headers.SetException(response.Exception);
            ActivityContextPropagation.InjectContext(Activity.Current, headers);

            return new RabbitMqMessage
            {
                Content = new StringContent(string.Empty),
                Properties = new RabbitMqMessageProperties
                {
                    CorrelationId = correlationId
                },
                Mandatory = false,
                Persistent = requestMessage.Persistent,
                Headers = headers,
                RoutingKey = replyTo,
                Exchange = null
            };
        }
    }
}