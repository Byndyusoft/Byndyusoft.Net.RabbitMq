using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public delegate Task AfterRabbitQueueConsumerStopEventHandler(IRabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public delegate Task BeforeRabbitQueueConsumerStartEventHandler(IRabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public delegate Task<ConsumeResult>
        ReceivedRabbitMqMessageHandler(ReceivedRabbitMqMessage message, CancellationToken cancellationToken);

    public interface IRabbitMqConsumer : IDisposable
    {
        string QueueName { get; }
        IRabbitMqClient Client { get; }
        bool? Exclusive { get; set; }
        ushort? PrefetchCount { get; set; }
        ReceivedRabbitMqMessageHandler OnMessage { get; set; }
        bool IsRunning { get; }
        event BeforeRabbitQueueConsumerStartEventHandler OnStarting;
        event AfterRabbitQueueConsumerStopEventHandler OnStopped;
        Task<IRabbitMqConsumer> StartAsync(CancellationToken cancellationToken = default);
        Task<IRabbitMqConsumer> StopAsync(CancellationToken cancellationToken = default);
    }
}