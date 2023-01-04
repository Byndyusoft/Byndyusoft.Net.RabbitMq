using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class SubscribeAsExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public SubscribeAsExample(IRabbitMqClient rabbitMqClient)
        {
            _rabbitMqClient = rabbitMqClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "as-json-example";

            using var consumer = _rabbitMqClient.SubscribeAs<Message>(queueName,
                    (model, _) =>
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        return Task.FromResult(ConsumeResult.Ack);
                    })
                .WithPrefetchCount(20)
                .WithDeclareSubscribingQueue(options => options.AsAutoDelete(true))
                .Start();

            await Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var message = new Message { Property = "json-example" };
                    await _rabbitMqClient.PublishAsJsonAsync(null, queueName, message, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);
        }
    }
}