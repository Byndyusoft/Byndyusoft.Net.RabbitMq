using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientCoreOptionsExtensions
    {
        public static string GetQueueName(this RabbitMqClientCoreOptions options, string exchange, string routingKey)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.NamingConventions.QueueName(exchange, routingKey, options.ApplicationName);
        }

        public static string GetRpcReplyQueueName(this RabbitMqClientCoreOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.NamingConventions.RpcReplyQueueName(options.ApplicationName);
        }
    }
}