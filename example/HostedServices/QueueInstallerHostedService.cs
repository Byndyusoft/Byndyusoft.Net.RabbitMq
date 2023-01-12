using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq.HostedServices
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
                        .Subscribe("test", (message, _) =>
                        {
                            Console.WriteLine($"Retry count {message.RetryCount}");

                            if (message.RetryCount > 0)
                                return Task.FromResult(ConsumeResult.Retry());

                            throw new Exception("first retry");
                        })
                        .WithDeclareSubscribingQueue(QueueOptions.Default)
                        .WithDeclareErrorQueue(QueueOptions.Default)
                        .WithConstantTimeoutRetryStrategy(TimeSpan.FromSeconds(10), 5, QueueOptions.Default)
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