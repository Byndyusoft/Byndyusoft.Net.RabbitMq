using System;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class RabbitMessageFactory
    {
        public static async Task<(byte[] body, MessageProperties properties)> CreateRabbitMessageAsync(
            QueueMessage message)
        {
            var body = await message.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var properties = CreateRabbitMessageProperties(message);
            return new ValueTuple<byte[], MessageProperties>(body, properties);
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
    }
}