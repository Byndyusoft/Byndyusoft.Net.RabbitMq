using System;
using System.Text;
using Byndyusoft.Messaging.RabbitMq.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byndyusoft.Messaging.RabbitMq.Native.Internal
{
    internal static class ReceivedRabbitMqMessageFactory
    {
        public static ReturnedRabbitMqMessage CreateReturnedMessage(
            BasicReturnEventArgs args)
        {
            var properties = CreateMessageProperties(args.BasicProperties);
            var headers = CreateMessageHeaders(args.BasicProperties);

            return new ReturnedRabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(args.Body, properties),
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                Properties = properties,
                Headers = headers,
                ReturnReason = args.ReplyCode.ToString()
            };
        }

        public static ReceivedRabbitMqMessage CreateReceivedMessage(
            string queueName,
            BasicDeliverEventArgs args)
        {
            var properties = CreateMessageProperties(args.BasicProperties);
            var headers = CreateMessageHeaders(args.BasicProperties);
            var retryCount = args.BasicProperties.Headers.GetRetryCount() ?? 0;

            return new ReceivedRabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(args.Body, properties),
                ConsumerTag = args.ConsumerTag,
                DeliveryTag = args.DeliveryTag,
                Queue = queueName,
                Redelivered = args.Redelivered,
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                Properties = properties,
                Headers = headers,
                RetryCount = retryCount,
                Persistent = args.BasicProperties.DeliveryMode == 2
            };
        }

        public static ReceivedRabbitMqMessage? CreatePulledMessage(
            string queueName,
            BasicGetResult? getResult)
        {
            if (getResult is null)
                return null;

            var body = getResult.Body;
            var messageProperties = getResult.BasicProperties;

            var properties = CreateMessageProperties(messageProperties);
            var headers = CreateMessageHeaders(messageProperties);
            var retryCount = messageProperties.Headers.GetRetryCount() ?? 0;

            return new ReceivedRabbitMqMessage
            {
                Content = RabbitMqMessageContent.Create(body, properties),
                ConsumerTag = string.Empty,
                DeliveryTag = getResult.DeliveryTag,
                Queue = queueName,
                Redelivered = getResult.Redelivered,
                RoutingKey = getResult.RoutingKey,
                Exchange = getResult.Exchange,
                Properties = properties,
                Headers = headers,
                RetryCount = retryCount,
                Persistent = messageProperties.DeliveryMode == 2
            };
        }

        public static RabbitMqMessageHeaders CreateMessageHeaders(IBasicProperties properties)
        {
            var headers = new RabbitMqMessageHeaders();
            if (properties.Headers is null)
                return headers;

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

        public static RabbitMqMessageProperties CreateMessageProperties(IBasicProperties properties)
        {
            var expiration =
                string.IsNullOrWhiteSpace(properties.Expiration)
                    ? (TimeSpan?) null
                    : TimeSpan.FromMilliseconds(int.Parse(properties.Expiration));

            return new()
            {
                ContentEncoding = properties.ContentEncoding,
                ContentType = properties.ContentType,
                CorrelationId = properties.CorrelationId,
                Expiration = expiration,
                MessageId = properties.MessageId,
                Priority = properties.Priority,
                Type = properties.Type,
                ReplyTo = properties.ReplyTo,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(properties.Timestamp.UnixTime).DateTime,
                UserId = properties.UserId,
                AppId = properties.AppId
            };
        }
    }
}