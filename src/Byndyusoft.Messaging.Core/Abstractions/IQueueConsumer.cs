using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.Abstractions
{
    public interface IQueueConsumer : IDisposable
    {
        string QueueName { get; }

        bool? Exclusive { get; }

        ushort? PrefetchCount { get; }

        ValueTask<IQueueConsumer> StartAsync(CancellationToken cancellationToken = default);

        ValueTask<IQueueConsumer> StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Switch a consumer to exclusive mode</summary>
        IQueueConsumer WithExclusive(bool exclusive);

        /// <summary>Sets prefetch count</summary>
        IQueueConsumer WithPrefetchCount(ushort prefetchCount);
    }
}