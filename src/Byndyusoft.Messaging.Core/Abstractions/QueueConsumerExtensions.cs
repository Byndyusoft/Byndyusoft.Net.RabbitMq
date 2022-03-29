using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public static class QueueConsumerExtensions
    {
        public static void Start(this IQueueConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StartAsync().GetAwaiter().GetResult();
        }

        public static void Stop(this IQueueConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StopAsync().GetAwaiter().GetResult();
        }
    }
}