using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc cref="ITopologyConfigurator" />
    internal sealed class TopologyConfigurator : ITopologyConfigurator
    {
        /// <summary>
        ///     Connection and topology configuration
        /// </summary>
        private readonly RabbitMqConfiguration _configuration;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="configuration">Connection and topology configuration</param>
        public TopologyConfigurator(RabbitMqConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public ITopologyConfigurator Exchange(string exchangeName, Action<IExchangeConfigurator> setupExchange)
        {
            if (setupExchange == null) throw new ArgumentNullException(nameof(setupExchange));
            if (string.IsNullOrWhiteSpace(exchangeName))

                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            _configuration.AddExchange(exchangeName);
            var exchangeConfigurator = new ExchangeConfigurator(_configuration.ExchangeConfigurations[exchangeName]);
            setupExchange(exchangeConfigurator);
            return this;
        }
    }
}