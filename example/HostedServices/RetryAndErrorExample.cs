using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class RetryAndErrorExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public RetryAndErrorExample(IRabbitMqClient rabbitMqClient)
        {
            _rabbitMqClient = rabbitMqClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "retry-example";

            using var consumer = _rabbitMqClient.Subscribe(queueName,
                    async (queueMessage, cancellationToken) =>
                    {
                        var model = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine($"{JsonConvert.SerializeObject(model)}, Retried: {queueMessage.RetryCount}");

                        return ConsumeResult.Retry();
                    })
                .WithPrefetchCount(20)
                .WithDeclareSubscribingQueue(options => options.AsAutoDelete(true))
                .WithDeclareErrorQueue(option => option.AsAutoDelete(true))
                .WithConstantTimeoutRetryStrategy(TimeSpan.FromSeconds(10), 5, options => options.AsAutoDelete(true))
                .Start();

            var message = new RabbitMqMessage
            {
                RoutingKey = queueName,
                Content = JsonContent.Create(new Message {Property = "retry-example"})
            };

            await _rabbitMqClient.PublishMessageAsync(message, stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}