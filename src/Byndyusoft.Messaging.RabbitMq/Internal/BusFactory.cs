using Byndyusoft.Messaging.RabbitMq.Abstractions;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    public class BusFactory : IBusFactory
    {
        public virtual IBus CreateBus(RabbitMqClientOptions options, ConnectionConfiguration connectionConfiguration)
        {
            connectionConfiguration.Name = options.ApplicationName;
            
            var builder = new ServiceCollection();
            builder.AddEasyNetQ(_ => connectionConfiguration);
            builder.AddSingleton<ISerializer>(new FakeSerializer());
            var provider = builder.BuildServiceProvider();
            return provider.GetRequiredService<IBus>();
        }
    }
}