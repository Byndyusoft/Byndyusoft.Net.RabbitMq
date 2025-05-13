using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class SubscribeAsJsonExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public SubscribeAsJsonExample(IRabbitMqClientFactory rabbitMqClientFactory)
        {
            _rabbitMqClient = rabbitMqClientFactory.CreateClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string queueName = "json-example";

            using var consumer = _rabbitMqClient.SubscribeAsJson<Message>(queueName,
                    (model, _) =>
                    {
                        Console.WriteLine(JsonSerializer.Serialize(model));
                        return Task.FromResult(ConsumeResult.Ack);
                    })
                .WithPrefetchCount(1)
                .WithDeclareSubscribingQueue(options => options.AsAutoDelete(true))
                .Start();

            await Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var message = new Message {Property = "json-example"};
                    await _rabbitMqClient.PublishAsJsonAsync(null, queueName, message, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);
        }
    }
}