using Byndyusoft.Messaging.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(ConnectionConfiguration connectionConfiguration)
        {
            return RabbitHutch.CreateBus(connectionConfiguration, _ => { });
        }
    }
}