using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(ConnectionConfiguration connectionConfiguration)
        {
            return RabbitHutch.CreateBus(
                connectionConfiguration,
                services => services.Register<IConventions>(
                    c => new NamingConventions(c.Resolve<ITypeNameSerializer>())));
        }
    }
}