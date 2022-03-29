using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(ConnectionConfiguration connectionConfiguration)
        {
            return RabbitHutch.CreateBus(connectionConfiguration, _ => { });
        }
    }
}