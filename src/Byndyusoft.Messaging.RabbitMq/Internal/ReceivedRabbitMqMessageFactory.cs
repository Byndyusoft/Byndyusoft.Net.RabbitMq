using System;
using System.Linq;
using System.Text;
using Byndyusoft.Messaging.RabbitMq.Messages;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class ReceivedRabbitMqMessageFactory
    {
        private static int _counter;
        private static byte[]? _content;

        public static ReceivedRabbitMqMessage CreateReceivedMessage(byte[] body,
            MessageProperties messageProperties,
            MessageReceivedInfo info)
        {
            if (info.Queue == "pulling-example")
            {
                _counter++;

                if (_content == null)
                {
                    _content = body;
                }
                else if (body.SequenceEqual(_content) == false)
                {
                    var x = 10;
                }
            }

            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = messageProperties.Headers.GetRetryCount() ?? 0;

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

        public static ReceivedRabbitMqMessage? CreateReceivedMessage(PullResult pullResult)
        {
            if (pullResult.IsAvailable == false)
                return null;

            return CreateReceivedMessage(pullResult.Body, pullResult.Properties, pullResult.ReceivedInfo);
        }
    }
}