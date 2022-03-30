using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.Abstractions
{
    public delegate Task BeforeQueueConsumerStartEventHandler(IQueueConsumer consumer,
        CancellationToken cancellationToken);

    public delegate Task AfterQueueConsumerStopEventHandler(IQueueConsumer consumer,
        CancellationToken cancellationToken);

    public interface IQueueConsumer : IDisposable
    {
        string QueueName { get; }

        bool? Exclusive { get; }

        ushort? PrefetchCount { get; }

        public bool IsRunning { get; }

        public IQueueService QueueService { get; }

        public event BeforeQueueConsumerStartEventHandler? BeforeStart;

        public event AfterQueueConsumerStopEventHandler? AfterStop;

        ValueTask StartAsync(CancellationToken cancellationToken = default);

        ValueTask StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Switch a consumer to exclusive mode</summary>
        IQueueConsumer WithExclusive(bool exclusive);

        /// <summary>Sets prefetch count</summary>
        IQueueConsumer WithPrefetchCount(ushort prefetchCount);
    }
}