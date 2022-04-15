using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public delegate Task AfterRabbitQueueConsumerStopEventHandler(IRabbitMqConsumer consumer, CancellationToken cancellationToken);

    public delegate Task BeforeRabbitQueueConsumerStartEventHandler(IRabbitMqConsumer consumer, CancellationToken cancellationToken);

    public interface IRabbitMqConsumer : IDisposable
    {
        bool IsRunning { get; }
        string QueueName { get; }
        IRabbitMqClient Client { get; }
        bool? Exclusive { get; set; }
        ushort? PrefetchCount { get; set; }
        event BeforeRabbitQueueConsumerStartEventHandler OnStarting;
        event AfterRabbitQueueConsumerStopEventHandler OnStopped;
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}