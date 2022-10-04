using Byndyusoft.Messaging.RabbitMq.Abstractions;
using EasyNetQ;
using EasyNetQ.DI;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(ConnectionConfiguration connectionConfiguration)
        {
            return RabbitHutch.CreateBus(connectionConfiguration,
                register => register.TryRegister<ISerializer>(_ => new MockSerializer()));
        }
    }
}