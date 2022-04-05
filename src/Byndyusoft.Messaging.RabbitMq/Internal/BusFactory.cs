using Byndyusoft.Messaging.RabbitMq.Abstractions;
using EasyNetQ;
using EasyNetQ.Logging;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    public class BusFactory : IBusFactory
    {
        static BusFactory()
        {
            LogProvider.IsDisabled = true;
        }

        public virtual IBus CreateBus(ConnectionConfiguration connectionConfiguration)
        {
            return RabbitHutch.CreateBus(connectionConfiguration, _ => { });
        }
    }
}