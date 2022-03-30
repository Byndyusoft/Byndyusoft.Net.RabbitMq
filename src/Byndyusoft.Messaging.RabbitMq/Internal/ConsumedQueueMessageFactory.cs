using System;
using System.Net.Http;
using System.Text;
using Byndyusoft.Messaging.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class ConsumedQueueMessageFactory
    {
        public static ConsumedQueueMessage CreateConsumedMessage(byte[] body, MessageProperties messageProperties,
            MessageReceivedInfo info)
        {
            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = messageProperties.Headers.GetRetryCount() ?? 0;

            return new ConsumedQueueMessage
            {
                Content = RabbitMessageContent.Create(new ByteArrayContent(body)),
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

        public static QueueMessageHeaders CreateMessageHeaders(MessageProperties properties)
        {
            var headers = new QueueMessageHeaders();

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

        public static QueueMessageProperties CreateMessageProperties(MessageProperties properties)
        {
            return new()
            {
                ContentEncoding = properties.ContentEncoding,
                ContentType = properties.ContentType,
                CorrelationId = properties.CorrelationId,
                Expiration = properties.ExpirationPresent
                    ? TimeSpan.FromMilliseconds(int.Parse(properties.Expiration))
                    : null,
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

        public static ConsumedQueueMessage? CreateConsumedMessage(PullResult pullResult)
        {
            if (pullResult.IsAvailable == false)
                return null;

            return CreateConsumedMessage(pullResult.Body, pullResult.Properties, pullResult.ReceivedInfo);
        }
    }
}