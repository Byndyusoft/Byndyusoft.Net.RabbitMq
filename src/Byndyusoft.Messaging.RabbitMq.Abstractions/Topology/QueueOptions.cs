using System.Collections.Generic;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions.Topology
{
    public class QueueOptions
    {
        public static QueueOptions Default => new QueueOptions().AsDurable(true);

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
    }
}