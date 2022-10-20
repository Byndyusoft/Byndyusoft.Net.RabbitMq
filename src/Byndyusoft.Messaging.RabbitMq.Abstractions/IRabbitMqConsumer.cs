using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public delegate Task BeforeRabbitQueueConsumerStartDelegate(IRabbitMqConsumer consumer,
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
        Task<IRabbitMqConsumer> StartAsync(CancellationToken cancellationToken = default);
        Task<IRabbitMqConsumer> StopAsync(CancellationToken cancellationToken = default);
        IRabbitMqConsumer RegisterBeforeStartAction(BeforeRabbitQueueConsumerStartDelegate action, int priority = int.MaxValue);
    }
}