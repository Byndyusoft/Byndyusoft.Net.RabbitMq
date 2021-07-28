using System;
using System.Collections.Generic;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Настройки очереди
    /// </summary>
    public sealed  class QueueConfiguration
    {
        /// <summary>
        ///     Тип сообщений
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        ///     Имя очереди
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        ///     Ключ маршрутизации
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Типы врапперов\миддлвар для обработки сообщения
        /// </summary>
        public List<Type> Wrapers { get; }

        /// <summary>
        ///     Типы пайпов для обработки ошибок
        /// </summary>
        public List<Type> Pipes { get; }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="queueName">Имя очереди</param>
        /// <param name="routingKey">Ключ маршрутизации</param>
        /// <param name="messageType">Тип сообщений</param>
        public QueueConfiguration(string queueName, string routingKey, Type messageType)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));
            if (string.IsNullOrWhiteSpace(routingKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(routingKey));

            QueueName = queueName;
            RoutingKey = routingKey;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Wrapers = new List<Type>();
            Pipes = new List<Type>();
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