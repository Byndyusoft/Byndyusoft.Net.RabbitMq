using Byndyusoft.Messaging.Core;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitQueueService : QueueService
    {
        public RabbitQueueService(string connectionString)
            : base(new RabbitQueueServiceHandler(connectionString))
        {
        }
    }
}