using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class ClientFactoryExample : BackgroundService
    {
        private readonly IRabbitMqClientFactory _rabbitMqClientFactory;

        public ClientFactoryExample(IRabbitMqClientFactory rabbitMqClientFactory)
        {
            _rabbitMqClientFactory = rabbitMqClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "client-factory-example";

            var rabbitClient = _rabbitMqClientFactory.CreateClient("client-factory");

            await rabbitClient.CreateQueueAsync(queueName, options => options.AsAutoDelete(true), stoppingToken);
            await rabbitClient.CreateQueueAsync(rabbitClient.Options.NamingConventions.ErrorQueueName(queueName),
                options => options.AsAutoDelete(true), stoppingToken);

            var getTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    try
                    {
                        using var message = await rabbitClient.GetMessageAsync(queueName, stoppingToken);
                        if (message is not null)
                            try
                            {
                                var model = await message.Content.ReadFromJsonAsync<Message>(
                                    cancellationToken: stoppingToken);
                                Console.WriteLine(JsonConvert.SerializeObject(model));
                                await rabbitClient.CompleteMessageAsync(message, ConsumeResult.Ack, stoppingToken);
                                continue;
                            }
                            catch (Exception e)
                            {
                                await rabbitClient.CompleteMessageAsync(message, ConsumeResult.Error(e),
                                    stoppingToken);
                            }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);

            var publishTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var model = new Message {Property = queueName};
                    await rabbitClient.PublishAsJsonAsync(null, queueName, model, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);

            await Task.WhenAll(getTask, publishTask);
        }
    }
}