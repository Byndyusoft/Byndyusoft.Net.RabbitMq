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
        ///     Connection string (for example 'host=localhost')
        /// </summary>
        public string?  ConnectionString { get; set; }

        /// <summary>
        ///     Configuration of exchanges of bound queues
        /// </summary>
        public Dictionary<string, ExchangeConfiguration> ExchangeConfigurations { get; }

        /// <summary>
        ///     EasyNetQ dependency injector
        /// </summary>
        public Action<IServiceRegister>? Register { get; set; }

        /// <summary>
        ///     Ctor
        /// </summary>
        public RabbitMqConfiguration()
        {
            ExchangeConfigurations = new Dictionary<string, ExchangeConfiguration>();
        }

        /// <summary>
        ///      Add exchange configuration
        /// </summary>
        /// <param name="exchangeName"></param>
        public void AddExchange(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            if (ExchangeConfigurations.ContainsKey(exchangeName))
            {
                throw new Exception($"Exchange {exchangeName} has been already added");
            }

            ExchangeConfigurations.Add(exchangeName, new ExchangeConfiguration(exchangeName));
        }
    }
}