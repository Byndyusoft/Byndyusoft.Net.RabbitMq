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
using OpenTracing.Util;
using Tracer = Jaeger.Tracer;

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

            var message = new QueueMessage
            {
                RoutingKey = "queue",
                Content = JsonContent.Create(new Message
                {
                    Property = "property1"
                }),
                Headers = new QueueMessageHeaders
                {
                    {"header-key", "header-value"}
                }
            };

            GlobalTracer.Register(new Tracer.Builder("Byndyusoft.Net.RabbitMq").Build());

            var span = GlobalTracer.Instance.BuildSpan(nameof(Main)).StartActive(true);

            using var activity = ActivitySource.StartActivity(nameof(Main));
            activity?.AddBaggage("baggage-key1", "baggage-value1");
            activity?.AddBaggage("baggage-key2", "baggage-value2");

            service.Subscribe("queue",
                    async (queueMessage, cancellationToken) =>
                    {
                        Console.WriteLine(queueMessage.RetryCount);

                        if (queueMessage.RetryCount == 5)
                            return ConsumeResult.Error;

                        var msg = await queueMessage.Content.ReadAsAsync<Message>(cancellationToken);
                        Console.WriteLine(JsonConvert.SerializeObject(msg));

                        return ConsumeResult.Retry;
                    })
                .WithRetryTimeout(TimeSpan.FromSeconds(60));

            await service.PublishBatchAsync(new[] {message});

            activity?.Dispose();
            span?.Dispose();

            Console.ReadKey();
        }
    }
}