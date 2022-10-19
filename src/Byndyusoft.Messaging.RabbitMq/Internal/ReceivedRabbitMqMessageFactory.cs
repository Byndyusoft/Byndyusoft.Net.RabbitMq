using System;
using System.Text;
using Byndyusoft.Messaging.RabbitMq.Messages;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class ReceivedRabbitMqMessageFactory
    {
        public static ReturnedRabbitMqMessage CreateReturnedMessage(
            ReadOnlyMemory<byte> body,
            MessageProperties messageProperties,
            MessageReturnedInfo info)
        {
            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);

            return new ReturnedRabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(body, properties),
                RoutingKey = info.RoutingKey,
                Exchange = info.Exchange,
                Properties = properties,
                Headers = headers,
                ReturnReason = info.ReturnReason
            };
        }

        public static ReceivedRabbitMqMessage CreateReceivedMessage(
            ReadOnlyMemory<byte> body,
            MessageProperties messageProperties,
            MessageReceivedInfo info)
        {
            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = headers.GetRetryCount() ?? 0;

            return new ReceivedRabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(body, properties),
                ConsumerTag = info.ConsumerTag,
                DeliveryTag = info.DeliveryTag,
                Queue = info.Queue,
                Redelivered = info.Redelivered,
                RoutingKey = info.RoutingKey,
                Exchange = info.Exchange,
                Properties = properties,
                Headers = headers,
                RetryCount = retryCount,
                Persistent = messageProperties.DeliveryMode == 2
            };
        }

        public static RabbitMqMessageHeaders CreateMessageHeaders(MessageProperties properties)
        {
            var headers = new RabbitMqMessageHeaders();

            foreach (var header in properties.Headers)
            {
                var value = header.Value switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    _ => header.Value
                };

                if (value is not null) headers.Add(header.Key, value);
            }

            return headers;
        }

        public static RabbitMqMessageProperties CreateMessageProperties(MessageProperties properties)
        {
            return new()
            {
                ContentEncoding = properties.ContentEncoding,
                ContentType = properties.ContentType,
                CorrelationId = properties.CorrelationId,
                Expiration = properties.Expiration,
                MessageId = properties.MessageId,
                Priority = properties.PriorityPresent ? properties.Priority : null,
                Type = properties.Type,
                ReplyTo = properties.ReplyTo,
                Timestamp = properties.TimestampPresent
                    ? DateTimeOffset.FromUnixTimeMilliseconds(properties.Timestamp).DateTime
                    : null,
                UserId = properties.UserId,
                AppId = properties.AppId
            };
        }

        public static PulledRabbitMqMessage? CreatePulledMessage(PullResult pullResult, IRabbitMqClientHandler handler)
        {
            if (pullResult.IsAvailable == false)
                return null;

            var info = pullResult.ReceivedInfo;
            var body = pullResult.Body;
            var messageProperties = pullResult.Properties;

            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = headers.GetRetryCount() ?? 0;

            return new PulledRabbitMqMessage(handler)
            {
                Content = RabbitMqMessageContent.Create(body, properties),
                ConsumerTag = info.ConsumerTag,
                DeliveryTag = info.DeliveryTag,
                Queue = info.Queue,
                Redelivered = info.Redelivered,
                RoutingKey = info.RoutingKey,
                Exchange = info.Exchange,
                Properties = properties,
                Headers = headers,
                RetryCount = retryCount,
                Persistent = messageProperties.DeliveryMode == 2
            };
        }
    }
}