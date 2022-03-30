using Byndyusoft.Messaging.Abstractions;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitQueueService : RabbitQueueService
    {
        public InMemoryRabbitQueueService(QueueServiceOptions options) 
            : base(new InMemoryRabbitQueueServiceHandler(options))
        {
        }

        public InMemoryRabbitQueueService(InMemoryRabbitQueueServiceHandler handler) : base(handler)
        {
        }
    }
}