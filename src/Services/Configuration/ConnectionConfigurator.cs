using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

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
        public ITopologyConfigurator Connection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

            _configuration.ConnectionString = connectionString;
            return new TopologyConfigurator(_configuration);
        }
    }
}