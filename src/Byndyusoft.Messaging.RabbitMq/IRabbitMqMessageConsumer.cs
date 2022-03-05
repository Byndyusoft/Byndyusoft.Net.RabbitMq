using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqMessageConsumer : IDisposable
    {
        string QueueName { get; }

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}