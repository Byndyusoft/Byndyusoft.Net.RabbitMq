namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClient
    {
        public InMemoryRabbitMqClient(RabbitMqClientOptions options)
            : base(new InMemoryRabbitMqClientHandler(options))
        {
        }

        public InMemoryRabbitMqClient(InMemoryRabbitMqClientHandler handler) : base(handler)
        {
        }
    }
}