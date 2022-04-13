using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Core;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Byndyusoft.Net.RabbitMq
{
    public class Message
    {
        public string Property { get; set; } = default!;
    }

    public static class Program
    {
        private static readonly ActivitySource ActivitySource = new(nameof(Program));

        public static async Task Main()
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("Byndyusoft.Net.RabbitMq"))
                .SetSampler(new AlwaysOnSampler())
                .AddSource(ActivitySource.Name)
                .AddJaegerExporter(jaeger =>
                {
                    jaeger.AgentHost = "localhost";
                    jaeger.AgentPort = 6831;
                })
                .AddRabbitMqClientInstrumentation()
                .Build();

            var service = new ServiceCollection()
                .AddRabbitMqClient(options =>
                {
                    options.ConnectionString = "host=localhost;username=rabbitmq;password=rabbitmq";
                    //options.ApplicationName = "Byndyusoft.Net.RabbitMq";
                })
                .AddInMemoryRabbitMqClient()
                .BuildServiceProvider()
                .GetRequiredService<IRabbitMqClient>();

            await SubscribeExchangeExample(service);
            await SubscribeAsJsonExample(service);
            await RetryAndErrorExample(service);
            await PullingExample(service);

            Console.ReadKey();
        }

        public static async Task SubscribeExchangeExample(IRabbitMqClient service)
        {
            await service.CreateExchangeIfNotExistsAsync("exchange", ex => ex.AsAutoDelete(true));

            service.Subscribe("exchange", "routingKey",
                    async (queueMessage, cancellationToken) =>
                    {
                        var model = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        return ConsumeResult.Ack;
                    })
                .WithPrefetchCount(20)
                .WithQueue(options => options.AsAutoDelete(true))
                .WithErrorQueue(option => option.AsAutoDelete(true))
                .Start();

            var publishTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (true)
                {
                    var message = new Message {Property = "exchange-example"};
                    await service.PublishAsJsonAsync("exchange", "routingKey", message);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()));
                }
            });
        }

        public static async Task PullingExample(IRabbitMqClient service)
        {
            var queueName = "pulling-example";
            await service.CreateQueueAsync(queueName, options => options.AsAutoDelete(true));

            var getTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (true)
                {
                    using var message = await service.GetMessageAsync(queueName);
                    if (message is not null)
                    {
                        var model = await message.Content.ReadFromJsonAsync<Message>();
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        await service.CompleteMessageAsync(message, ConsumeResult.Ack);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()));
                    }
                }
            });

            var publishTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (true)
                {
                    var model = new Message {Property = "pulling-example"};
                    await service.PublishAsJsonAsync(null, queueName, model);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()));
                }
            });
        }

        public static async Task SubscribeAsJsonExample(IRabbitMqClient service)
        {
            var queueName = "json-example";

            service.SubscribeAsJson<Message>(queueName,
                    (model, _) =>
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        return Task.CompletedTask;
                    })
                .WithPrefetchCount(20)
                .WithQueue(options => options.AsAutoDelete(true))
                .Start();


            var publishTask = Task.Run(async () =>
            {
                var rand = new Random();
                while (true)
                {
                    var message = new Message {Property = "json-example"};
                    await service.PublishAsJsonAsync(null, queueName, message);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()));
                }
            });
        }

        public static async Task RetryAndErrorExample(IRabbitMqClient service)
        {
            var queueName = "retry-example";

            service.Subscribe(queueName,
                    async (queueMessage, cancellationToken) =>
                    {
                        var model = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine($"{JsonConvert.SerializeObject(model)}, Retried: {queueMessage.RetryCount}");

                        if (queueMessage.RetryCount == 5)
                            return ConsumeResult.Error;
                        return ConsumeResult.Retry;
                    })
                .WithPrefetchCount(20)
                .WithQueue(options => options.AsAutoDelete(true))
                .WithErrorQueue(option => option.AsAutoDelete(true))
                .WithRetryQueue(TimeSpan.FromSeconds(10), options => options.AsAutoDelete(true))
                .Start();

            var message = new RabbitMqMessage
            {
                RoutingKey = queueName,
                Content = JsonContent.Create(new Message {Property = "retry-example"})
            };
            await service.PublishMessageAsync(message);
        }
    }
}