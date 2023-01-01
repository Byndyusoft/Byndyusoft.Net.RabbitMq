using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Byndyusoft.Messaging.RabbitMq.Native.Internal
{
    internal static class RabbitMqMessageFactory
    {
        public static async Task<(byte[] body, IBasicProperties properties)> CreateMessage(IModel model, RabbitMqMessage message)
        {
            var body = await message.Content.ReadAsByteArrayAsync()
                .ConfigureAwait(false);

            var properties = CreateBasicProperties(model, message);
            return (body, properties);
        }

        private static IBasicProperties CreateBasicProperties(IModel model, RabbitMqMessage message)
        {
            var properties = model.CreateBasicProperties();
            properties.AppId = message.Properties.AppId;
            properties.ContentEncoding = message.Properties.ContentEncoding;
            properties.ContentType = message.Properties.ContentType;
            properties.CorrelationId = message.Properties.CorrelationId;
            properties.DeliveryMode = message.Persistent ? (byte)1 : (byte)2;
            properties.Headers = message.Headers;
            properties.MessageId = message.Properties.MessageId;
            properties.Persistent = message.Persistent;
            properties.ReplyTo = message.Properties.ReplyTo;
            properties.Type = message.Properties.Type;
            properties.UserId = message.Properties.UserId;

            if (message.Properties.Priority is not null) 
                properties.Priority = message.Properties.Priority.Value;

            if (message.Properties.Timestamp is not null)
                properties.Timestamp = new AmqpTimestamp(new DateTimeOffset(message.Properties.Timestamp.Value).ToUnixTimeMilliseconds());

            if (message.Properties.Expiration is not null)
                properties.Expiration = $"{(int)message.Properties.Expiration.Value.TotalMilliseconds}";

            return properties;
        }
    }
}