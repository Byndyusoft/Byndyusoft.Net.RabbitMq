using System;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    /// <summary>
    ///     Various extensions for <see cref="QueueOptions" />.
    /// </summary>
    public static class QueueOptionsExtensions
    {
        /// <summary>
        ///     Sets queue as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="maxPriority">The maxPriority to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithMaxPriority(this QueueOptions options, int maxPriority)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-max-priority", maxPriority);
        }

        /// <summary>
        ///     Sets maxLength. The maximum number of ready messages that may exist on the queue. Messages will be dropped or
        ///     dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="maxLength">The maxLength to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithMaxLength(this QueueOptions options, int maxLength)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-max-length", maxLength);
        }

        /// <summary>
        ///     Sets maxLengthBytes. The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the
        ///     front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="maxLengthBytes">The maxLengthBytes flag to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithMaxLengthBytes(this QueueOptions options, int maxLengthBytes)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-max-length-bytes", maxLengthBytes);
        }

        /// <summary>
        ///     Sets expires of the queue. Determines how long a queue can remain unused before it is automatically deleted by the
        ///     server.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="expires">The expires to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithExpires(this QueueOptions options, TimeSpan expires)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-expires", (int) expires.TotalMilliseconds);
        }

        /// <summary>
        ///     Sets messageTtl. Determines how long a message published to a queue can live before it is discarded by the server.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="messageTtl">The messageTtl to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithMessageTtl(this QueueOptions options, TimeSpan messageTtl)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-message-ttl", (int) messageTtl.TotalMilliseconds);
        }

        public static TimeSpan? GetMessageTtl(this QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            if (options.Arguments.TryGetValue("x-message-ttl", out var value) == false)
                return null;
            return TimeSpan.FromMilliseconds((int) value);
        }

        /// <summary>
        ///     Sets deadLetterExchange. Determines an exchange's name can remain unused before it is automatically deleted by the
        ///     server.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="deadLetterExchange">The deadLetterExchange to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithDeadLetterExchange(this QueueOptions options, string? deadLetterExchange)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-dead-letter-exchange", deadLetterExchange ?? string.Empty);
        }

        public static string? GetDeadLetterExchange(this QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            if (options.Arguments.TryGetValue("x-dead-letter-exchange", out var value) == false)
                return null;
            return (string?) value;
        }

        /// <summary>
        ///     Sets deadLetterRoutingKey. If set, will route message with the routing key specified, if not set, message will be
        ///     routed with the same routing keys they were originally published with.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="deadLetterRoutingKey">The deadLetterRoutingKey to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithDeadLetterRoutingKey(this QueueOptions options, string deadLetterRoutingKey)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-dead-letter-routing-key", deadLetterRoutingKey);
        }

        public static string? GetDeadLetterRoutingKey(this QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            if (options.Arguments.TryGetValue("x-dead-letter-routing-key", out var value) == false)
                return null;
            return (string?) value;
        }

        /// <summary>
        ///     Sets queueMode. Valid modes are default and lazy.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="queueMode">The queueMode to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithQueueMode(this QueueOptions options, string queueMode)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-queue-mode", queueMode);
        }

        /// <summary>
        ///     Sets queueType. Valid types are classic and quorum.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <param name="queueType">The queueType to set</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithQueueType(this QueueOptions options, QueueType queueType)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-queue-type", queueType);
        }

        /// <summary>
        ///     Enables single active consumer
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <returns>QueueOptions</returns>
        public static QueueOptions WithSingleActiveConsumer(this QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            return options.WithArgument("x-single-active-consumer", true);
        }
    }
}