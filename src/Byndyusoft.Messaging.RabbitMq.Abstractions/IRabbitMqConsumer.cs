using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public delegate Task AfterRabbitQueueConsumerStopEventHandler(IRabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public delegate Task BeforeRabbitQueueConsumerStartEventHandler(IRabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public interface IRabbitMqConsumer : IDisposable
    {
        bool IsRunning { get; }
        string QueueName { get; }
        bool? Exclusive { get; set; }
        ushort? PrefetchCount { get; set; }
        IRabbitMqClient Client { get; }
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        IRabbitMqConsumer WithQueue(QueueOptions options);
        IRabbitMqConsumer OnStarting(BeforeRabbitQueueConsumerStartEventHandler handler);
        IRabbitMqConsumer OnStopped(AfterRabbitQueueConsumerStopEventHandler handler);

        /// <summary>Switch a consumer to exclusive mode</summary>
        IRabbitMqConsumer WithExclusive(bool exclusive);

        /// <summary>Sets prefetch count</summary>
        IRabbitMqConsumer WithPrefetchCount(ushort prefetchCount);
    }
}