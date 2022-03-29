using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Core;
using Byndyusoft.Messaging.OpenTracing;
using Byndyusoft.Messaging.RabbitMq;
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
                .AddOpenTracingExporter(QueueServiceActivitySource.Name)
                .AddQueueServiceInstrumentation()
                .Build();

            var service = new ServiceCollection()
                .AddRabbitQueueService(options =>
                {
                    options.ConnectionString = "host=localhost;username=guest;password=guest";
                    options.ApplicationName = "Byndyusoft.Net.RabbitMq";
                })
                .BuildServiceProvider()
                .GetRequiredService<IRabbitQueueService>();

            await SubscribeExchangeExample(service);
            await SubscribeAsJsonExample(service);
            await RetryAndErrorExample(service);
            await PullingExample(service);

            Console.ReadKey();
        }

        public static async Task SubscribeExchangeExample(IRabbitQueueService service)
        {
            await service.CreateExchangeIfNotExistsAsync("exchange", ex =>  ex.AsAutoDelete(true));
            
            var message = new Message {Property = "exchange-example"};

            service.Subscribe("exchange", "routingKey",
                    async (queueMessage, cancellationToken) =>
                    {
                        var model = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        return ConsumeResult.Ack;
                    })
                .WithErrorQueue(option => option.AsAutoDelete(true))
                .WithQueue(options => options.AsAutoDelete(true))
                .WithRetryQueue(TimeSpan.FromSeconds(60), options =>  options.AsAutoDelete(true))
                .Start();

            await service.PublishAsJsonAsync("exchange", "routingKey", message);
        }

        public static async Task PullingExample(IRabbitQueueService service)
        {
            var queueName = Guid.NewGuid().ToString();
            await service.CreateQueueAsync(queueName, options =>  options.AsAutoDelete(true) );

            var getTask = Task.Run(async () =>
            {
                while (true)
                {
                    var message = await service.GetAsync(queueName);
                    if (message is not null)
                    {
                        var model = await message.Content.ReadFromJsonAsync<Message>();
                        Console.WriteLine(JsonConvert.SerializeObject(model));
                        await service.AckAsync(message);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
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
                    await Task.Delay(TimeSpan.FromSeconds(rand.Next(3, 10)));
                }
            });
        }

        public static async Task SubscribeAsJsonExample(IRabbitQueueService service)
        {
            var queueName = Guid.NewGuid().ToString();

            var message = new Message {Property = "json-example"};

            service.SubscribeAsJson<Message>(queueName,
                (msg, _) =>
                {
                    Console.WriteLine(JsonConvert.SerializeObject(msg));
                    return Task.CompletedTask;
                })
                .WithQueue(options => options.AsAutoDelete(true))
                .Start();


            await service.PublishAsJsonAsync(null, queueName, message);
        }

        public static async Task RetryAndErrorExample(IRabbitQueueService service)
        {
            var queueName = Guid.NewGuid().ToString();
          
            var message = new QueueMessage
            {
                RoutingKey = queueName,
                Content = JsonContent.Create(new Message {Property = "retry-example"})
            };

            service.Subscribe(queueName,
                    async (queueMessage, cancellationToken) =>
                    {
                        var model = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine($"{JsonConvert.SerializeObject(model)}, Retried: {queueMessage.RetryCount}");

                        if (queueMessage.RetryCount == 5)
                            return ConsumeResult.Error;
                        return ConsumeResult.Retry;
                    })
                .WithQueue(options => options.AsAutoDelete(true))
                .WithErrorQueue(option => option.AsAutoDelete(true))
                .WithRetryQueue(TimeSpan.FromSeconds(60), options => options.AsAutoDelete(true))
                .Start();

            await service.PublishAsync(message);
        }
    }
}