using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class RabbitMessageConverter
    {
        public static QueueMessage CreateRetryMessage(ConsumedQueueMessage consumedMessage, string retryQueueName)
        {
            return new()
            {
                Content = consumedMessage.Content,
                Properties = consumedMessage.Properties,
                Mandatory = false,
                Persistent = consumedMessage.Persistent,
                Headers = consumedMessage.Headers,
                RoutingKey = retryQueueName
            };
        }

        public static QueueMessage CreateErrorMessage(ConsumedQueueMessage consumedMessage, string errorQueueName,
            Exception? exception = null)
        {
            var headers = new QueueMessageHeaders(consumedMessage.Headers);
            if (exception is not null)
            {
                headers["exception-type"] = exception.GetType().FullName;
                headers["exception-message"] = exception.Message;
            }

            headers.Remove("x-death");
            headers.Remove("x-first-death-exchange");
            headers.Remove("x-first-death-queue");
            headers.Remove("x-first-death-reason");

            return new QueueMessage
            {
                Content = consumedMessage.Content,
                Properties = consumedMessage.Properties,
                Mandatory = false,
                Persistent = consumedMessage.Persistent,
                Headers = headers,
                RoutingKey = errorQueueName
            };
        }

        public static MessageProperties CreateRabbitMessageProperties(QueueMessage message)
        {
            var properties = new MessageProperties
            {
                Type = message.Properties.Type,
                DeliveryMode = (byte) (message.Persistent ? 2 : 1),
                ContentEncoding = message.Properties.ContentEncoding,
                ContentType = message.Properties.ContentType,
                AppId = message.Properties.AppId,
                CorrelationId = message.Properties.CorrelationId,
                MessageId = message.Properties.MessageId,
                ReplyTo = message.Properties.ReplyTo,
                UserId = message.Properties.UserId,
                Headers = message.Headers
            };

            if (message.Properties.Priority is not null) properties.Priority = message.Properties.Priority.Value;

            if (message.Properties.Timestamp is not null)
                properties.Timestamp = new DateTimeOffset(message.Properties.Timestamp.Value).ToUnixTimeMilliseconds();

            if (message.Properties.Expiration is not null)
                properties.Expiration = $"{(int) message.Properties.Expiration.Value.TotalMilliseconds}";

            return properties;
        }

        public static Task<byte[]> CreateRabbitMessageBodyAsync(QueueMessage message, CancellationToken _)
        {
            return message.Content.ReadAsByteArrayAsync();
        }

        public static ConsumedQueueMessage CreateConsumedMessage(byte[] body, MessageProperties messageProperties,
            MessageReceivedInfo info)
        {
            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = GetRetryCount(messageProperties);

            return new ConsumedQueueMessage
            {
                Content = new ByteArrayContent(body),
                ConsumerTag = info.ConsumerTag,
                DeliveryTag = info.DeliveryTag,
                Queue = info.Queue,
                Redelivered = info.Redelivered,
                RoutingKey = info.RoutingKey,
                Exchange = info.Exchange,
                Properties = properties,
                Headers = headers,
                RetryCount = (int) retryCount,
                Persistent = messageProperties.DeliveryMode == 2
            };
        }

        public static long GetRetryCount(MessageProperties properties)
        {
            if (properties.Headers.TryGetValue("x-death", out var value))
            {
                var retryInfo = (IDictionary<string, object>) ((List<object>) value)[0];
                return (long) retryInfo["count"];
            }

            return 0;
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