using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ.DI;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc cref="IConnectionConfigurator"/>
    internal sealed class ConnectionConfigurator : IConnectionConfigurator
    {
        /// <summary>
        ///     Full rabbit connection and topology configuration
        /// </summary>
        private readonly RabbitMqConfiguration _configuration;

        /// <summary>
        ///     Ctor
        /// </summary>
        public ConnectionConfigurator()
        {
            _configuration = new RabbitMqConfiguration();
        }

        /// <summary>
        ///     Returns full configuration
        /// </summary>
        public RabbitMqConfiguration Build()
        {
            return _configuration;
        }

        /// <inheritdoc />
        public IInjectionConfigurator Connection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

            _configuration.ConnectionString = connectionString;
            return new InjectionConfigurator(_configuration);
        }
    }

    /// <inheritdoc cref="IInjectionConfigurator"/>
    internal sealed class InjectionConfigurator : IInjectionConfigurator
    {
        private readonly RabbitMqConfiguration _configuration;

        public InjectionConfigurator(RabbitMqConfiguration configuration)
        {
            _configuration = configuration;
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
            return new TopologyConfigurator(_configuration);
        }

        public ITopologyConfigurator InjectServices(Action<IServiceRegister> register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            _configuration.Register = register;
            return new TopologyConfigurator(_configuration);
        }
    }
}