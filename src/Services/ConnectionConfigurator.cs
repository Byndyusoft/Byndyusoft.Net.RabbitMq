using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IConnectionConfigurator"/>
    internal sealed class ConnectionConfigurator : IConnectionConfigurator
    {
        /// <summary>
        ///     Возвращает конфигурации очередей
        /// </summary>
        public RabbitMqConfiguration Build()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public ITopologyConfigurator Connection(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}