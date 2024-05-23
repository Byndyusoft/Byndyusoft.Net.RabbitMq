namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClient : RabbitMqClient
    {
        public InMemoryRabbitMqClient()
            : base(new InMemoryRabbitMqClientHandler())
        {
        }
    }
}