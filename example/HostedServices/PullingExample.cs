using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class PullingExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public PullingExample(IRabbitMqClient rabbitMqClient)
        {
            _rabbitMqClient = rabbitMqClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "pulling-example";
            await _rabbitMqClient.CreateQueueAsync(queueName, options => options.AsAutoDelete(true), stoppingToken);
            await _rabbitMqClient.CreateQueueAsync(_rabbitMqClient.Options.NamingConventions.ErrorQueueName(queueName),
                options => options.AsAutoDelete(true), stoppingToken);

            var getTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    try
                    {
                        using var message = await _rabbitMqClient.GetMessageAsync(queueName, stoppingToken);
                        if (message is not null)
                            try
                            {
                                var model = await message.Content.ReadFromJsonAsync<Message>(
                                    cancellationToken: stoppingToken);
                                Console.WriteLine(JsonConvert.SerializeObject(model));
                                await _rabbitMqClient.CompleteMessageAsync(message, ConsumeResult.Ack, stoppingToken);
                                continue;
                            }
                            catch (Exception e)
                            {
                                await _rabbitMqClient.CompleteMessageAsync(message, ConsumeResult.Error(e),
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
                    var model = new Message {Property = "pulling-example"};
                    await _rabbitMqClient.PublishAsJsonAsync(null, queueName, model, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);

            await Task.WhenAll(getTask, publishTask);
        }
    }
}