using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientCoreOptionsExtensions
    {
        public static string GetQueueName(this RabbitMqClientOptions options, string exchange, string routingKey)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.NamingConventions.QueueName(exchange, routingKey, options.ApplicationName);
        }

        public static string GetRpcReplyQueueName(this RabbitMqClientOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.NamingConventions.RpcReplyQueueName(options.ApplicationName);
        }
    }
}