using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public static class QueueConsumerExtensions
    {
        public static IQueueConsumer Start(this IQueueConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StartAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static IQueueConsumer Stop(this IQueueConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StopAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static IQueueConsumer OnStarting(this IQueueConsumer consumer,
            Func<IQueueConsumer, CancellationToken, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.Check(consumer.IsRunning == false, "Can't change running consumer");

            consumer.BeforeStart += new BeforeQueueConsumerStartEventHandler(handler);
            return consumer;
        }

        public static IQueueConsumer OnStopped(this IQueueConsumer consumer,
            Func<IQueueConsumer, CancellationToken, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.Check(consumer.IsRunning == false, "Can't change running consumer");

            consumer.AfterStop += new AfterQueueConsumerStopEventHandler(handler);
            return consumer;
        }
    }
}