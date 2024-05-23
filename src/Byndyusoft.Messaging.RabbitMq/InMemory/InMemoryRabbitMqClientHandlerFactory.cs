namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClientHandlerFactory : IRabbitMqClientHandlerFactory
    {
        public IRabbitMqClientHandler CreateHandler(RabbitMqClientOptions options)
        {
            return new InMemoryRabbitMqClientHandler();
        }
    }
}