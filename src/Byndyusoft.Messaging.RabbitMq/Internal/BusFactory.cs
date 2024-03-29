using Byndyusoft.Messaging.RabbitMq.Abstractions;
using EasyNetQ;
using EasyNetQ.DI;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(RabbitMqClientOptions options, ConnectionConfiguration connectionConfiguration)
        {
            connectionConfiguration.Name = options.ApplicationName;
            return RabbitHutch.CreateBus(connectionConfiguration,
                register => register.TryRegister<ISerializer>(_ => new FakeSerializer()));
        }
    }
}