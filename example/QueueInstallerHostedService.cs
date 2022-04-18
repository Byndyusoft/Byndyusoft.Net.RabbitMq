using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Core;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq
{
    public class QueueInstallerHostedService : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public QueueInstallerHostedService(IRabbitMqClient rabbitMqClient)
        {
            _rabbitMqClient = rabbitMqClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    using var consumer = _rabbitMqClient
                        .Subscribe("test", (message, token) => Task.FromResult(ConsumeResult.Error))
                        .WithCreatingSubscribeQueue()
                        .WithSingleQueueRetry(TimeSpan.FromSeconds(10), QueueOptions.Default)
                        .Start();


                    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}