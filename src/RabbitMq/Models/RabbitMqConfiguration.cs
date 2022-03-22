using System;
using System.Collections.Generic;
using EasyNetQ.DI;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Connection and topology configuration for RabbitMq
    /// </summary>
    public sealed class RabbitMqConfiguration
    {
        /// <summary>
        ///     Ctor
        /// </summary>
        public RabbitMqConfiguration()
        {
            ExchangeConfigurations = new Dictionary<string, ExchangeConfiguration>();
        }

        /// <summary>
        ///     Connection string (for example 'host=localhost')
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        ///     Configuration of exchanges of bound queues
        /// </summary>
        public Dictionary<string, ExchangeConfiguration> ExchangeConfigurations { get; }

        /// <summary>
        ///     Delegate for overriding internal services of EasyNetQ
        /// </summary>
        public Action<IServiceRegister>? RegisterServices { get; internal set; }

        /// <summary>
        ///     Add exchange configuration
        /// </summary>
        public void AddExchange(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            if (ExchangeConfigurations.ContainsKey(exchangeName))
                throw new Exception($"Exchange {exchangeName} has been already added");

            ExchangeConfigurations.Add(exchangeName, new ExchangeConfiguration(exchangeName));
        }
    }
}