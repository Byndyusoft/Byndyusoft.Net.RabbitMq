using System;
using System.Collections.Generic;
using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Topology
{
    public class QueueOptions
    {
        private static Func<QueueOptions> _default = () => new QueueOptions().AsDurable(true);

        public static QueueOptions Default => _default();

        public QueueType Type { get; set; } = QueueType.Classic;

        public bool Durable { get; set; }

        public bool Exclusive { get; set; }

        public bool AutoDelete { get; set; }

        public Dictionary<string, object> Arguments { get; } = new();

        /// <summary>
        ///     Sets as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="isAutoDelete">The autoDelete flag to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions AsAutoDelete(bool isAutoDelete)
        {
            AutoDelete = isAutoDelete;
            return this;
        }

        /// <summary>
        ///     Sets as durable or not. Durable queues remain active when a server restarts.
        /// </summary>
        /// <param name="isDurable">The durable flag to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions AsDurable(bool isDurable)
        {
            Durable = isDurable;
            return this;
        }

        /// <summary>
        ///     Sets as exclusive or not. Exclusive queues may only be accessed by the current connection, and are deleted when
        ///     that connection closes.
        /// </summary>
        /// <param name="isExclusive">The exclusive flag to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions AsExclusive(bool isExclusive)
        {
            Exclusive = isExclusive;
            return this;
        }

        /// <summary>Sets a raw argument for query declaration</summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions WithArgument(string name, object value)
        {
            Arguments[name] = value;
            return this;
        }

        /// <summary>
        ///     Sets queue type.
        /// </summary>
        /// <param name="type">The queue type to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions WithType(QueueType type)
        {
            Type = type;
            return this;
        }

        /// <summary>
        ///     Sets queue mode.
        /// </summary>
        /// <param name="mode">The queue mode to set</param>
        /// <returns>QueueOptions</returns>
        public QueueOptions WithMode(QueueMode mode)
        {
            return WithArgument("x-queue-mode", mode.ToString().ToLowerInvariant());
        }

        /// <summary>
        ///     Sets Message TTL.
        /// </summary>
        /// <see href="https://www.rabbitmq.com/ttl.html#message-ttl-using-x-args" />
        public QueueOptions WithMessageTtl(TimeSpan messageTtl)
        {
            return WithArgument("x-message-ttl", (int)messageTtl.TotalMilliseconds);
        }

        /// <summary>
        ///     Sets queue TTL.
        /// </summary>
        /// <remarks>Queues will expire after a period of time only when they are not used (e.g. do not have consumers)</remarks>
        /// <see href="https://www.rabbitmq.com/ttl.html#queue-ttl" />
        public QueueOptions WithTtl(TimeSpan ttl)
        {
            return WithArgument("x-expires", (int)ttl.TotalMilliseconds);
        }

        /// <summary>
        ///     Sets max queue length.
        /// </summary>
        /// <see href="https://www.rabbitmq.com/maxlength.html#definition-using-x-args" />
        public QueueOptions WithMaxLength(int maxLength)
        {
            return WithArgument("x-max-length", maxLength);
        }

        /// <summary>
        ///     Sets max queue length in bytes.
        /// </summary>
        /// <see href="https://www.rabbitmq.com/maxlength.html#definition-using-x-args" />
        public QueueOptions WithMaxLengthBytes(int maxLengthBytes)
        {
            return WithArgument("x-max-length-bytes", maxLengthBytes);
        }

        /// <summary>
        ///     Sets queue overflow behaviour
        /// </summary>
        /// <see href="https://www.rabbitmq.com/maxlength.html#overflow-behaviour" />
        public QueueOptions WithOverflowBehaviour(QueueOverflowBehaviour behaviour)
        {
            string value = behaviour switch
            {
                QueueOverflowBehaviour.DropHead => "drop-head",
                QueueOverflowBehaviour.RejectPublish => "reject-publish",
                QueueOverflowBehaviour.RejectPublishDlx => "reject-publish-dlx",
                _ => throw new ArgumentOutOfRangeException(nameof(behaviour))
            };

            return WithArgument("x-overflow", value);
        }

        /// <summary>
        ///     Sets queue max priority
        /// </summary>
        /// <remarks>Max number of priorities: 255. Values between 1 and 10 are recommended.</remarks>
        /// <see href="https://www.rabbitmq.com/priority.html" />
        public QueueOptions WithMaxPriority(byte maxPriority)
        {
            return WithArgument("x-max-priority", (int)maxPriority);
        }

        public static void SetDefault(Func<QueueOptions> options)
        {
            _default = Preconditions.CheckNotNull(options, nameof(options));
        }
    }
}