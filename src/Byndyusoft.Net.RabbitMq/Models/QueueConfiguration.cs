using System;
using System.Collections.Generic;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Queue configuration
    /// </summary>
    public sealed  class QueueConfiguration
    {
        /// <summary>
        ///     Type of messages in queue
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        ///     Queue name
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        ///     Routing key for binding with exchange
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Types of middleware for producing or consuming messages from queue
        /// </summary>
        public List<Type> Middlewares { get; }

        /// <summary>
        ///     Types of middleware for handling returned messaged after producing
        /// </summary>
        public List<Type> ReturnedMiddlewares { get; }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="routingKey">Routing key for binding with exchange</param>
        /// <param name="messageType">Type of messages in queue</param>
        public QueueConfiguration(string queueName, string routingKey, Type messageType)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));
            if (string.IsNullOrWhiteSpace(routingKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(routingKey));

            QueueName = queueName;
            RoutingKey = routingKey;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Middlewares = new List<Type>();
            ReturnedMiddlewares = new List<Type>();
        }

        protected bool Equals(QueueConfiguration other)
        {
            return QueueName == other.QueueName && RoutingKey == other.RoutingKey;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QueueConfiguration) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (QueueName.GetHashCode() * 397) ^ RoutingKey.GetHashCode();
            }
        }
    }
}