namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClientCore
    {
        public InMemoryRabbitMqClient()
            : this(new RabbitMqClientOptions())
        {
        }

        public InMemoryRabbitMqClient(RabbitMqClientOptions options)
            : base(new InMemoryRabbitMqClientHandler(options))
        {
        }
    }
}