using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal static class RabbitMqMessageFactory
    {
        public static async Task<(byte[] body, MessageProperties properties)> CreateEasyNetQMessageAsync(
            RabbitMqMessage message)
        {
            var body = await message.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var properties = CreateEasyNetQMessageProperties(message);
            return new ValueTuple<byte[], MessageProperties>(body, properties);
        }

        public static MessageProperties CreateEasyNetQMessageProperties(RabbitMqMessage message)
        {
            long timestamp = 0;
            if (message.Properties.Timestamp is not null)
            {
                timestamp = new DateTimeOffset(message.Properties.Timestamp.Value).ToUnixTimeMilliseconds();
            }
            return new MessageProperties
            {
                Type = message.Properties.Type,
                DeliveryMode = (byte)(message.Persistent ? 2 : 1),
                ContentEncoding = message.Properties.ContentEncoding,
                ContentType = message.Properties.ContentType,
                AppId = message.Properties.AppId,
                CorrelationId = message.Properties.CorrelationId,
                MessageId = message.Properties.MessageId,
                ReplyTo = message.Properties.ReplyTo,
                UserId = message.Properties.UserId,
                Headers = message.Headers,
                Priority = message.Properties.Priority ?? 0,
                Expiration = message.Properties.Expiration,
                Timestamp = timestamp
            };
        }
    }
}