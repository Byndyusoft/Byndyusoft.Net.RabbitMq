using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IBusFactory" />
    public sealed class BusFactory : IBusFactory
    {
        /// <inheritdoc />
        public IBus CreateBus(RabbitMqConfiguration configuration)
        {
            if (configuration.RegisterServices != null)
                return RabbitHutch.CreateBus(configuration.ConnectionString, configuration.RegisterServices);
            return RabbitHutch.CreateBus(configuration.ConnectionString);
        }
    }
}