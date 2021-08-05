using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IBusFactory"/>
    public sealed class BusFactory : IBusFactory
    {
        public IBus CreateBus(string connectionString)
        {
            return RabbitHutch.CreateBus(connectionString);
        }
    }
}