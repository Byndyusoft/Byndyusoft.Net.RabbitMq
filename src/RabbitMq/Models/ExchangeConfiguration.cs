using System;
using System.Collections.Generic;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Configurations of exchange and bound queues
    /// </summary>
    public sealed class ExchangeConfiguration
    {
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="exchangeName">Exchange name</param>
        public ExchangeConfiguration(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            ExchangeName = exchangeName;
            ConsumeQueueConfigurations = new HashSet<QueueConfiguration>();
            ProduceQueueConfigurations = new HashSet<QueueConfiguration>();
        }

        /// <summary>
        ///     Exchange name
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        ///     Configurations of incoming queues
        /// </summary>
        public HashSet<QueueConfiguration> ConsumeQueueConfigurations { get; }

        /// <summary>
        ///     Configurations of outgoing queues
        /// </summary>
        public HashSet<QueueConfiguration> ProduceQueueConfigurations { get; }
    }
}