using System;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Core;
using Byndyusoft.Messaging.OpenTracing;
using Byndyusoft.Messaging.RabbitMq;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTracing.Util;

namespace Byndyusoft.Net.RabbitMq
{
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

            var service = new RabbitQueueService("host=localhost;username=rabbitmq;password=rabbitmq");
            var message = new QueueMessage
            {
                RoutingKey = "dead",
                Content = JsonContent.Create(new
                {
                    Property1 = "property1"
                }),
                Properties = new QueueMessageProperties
                {
                    Expiration = TimeSpan.FromSeconds(10),
                    Timestamp = DateTime.UtcNow,
                    Priority = 3
                },
                Headers = new QueueMessageHeaders
                {
                    {"header-key", "header-value"}
                }
            };

            var message2 = new QueueMessage
            {
                RoutingKey = "dead",
                Content = JsonContent.Create(new
                {
                    Property1 = "property2"
                }),
                Properties = new QueueMessageProperties
                {
                    Expiration = TimeSpan.FromSeconds(5),
                    Timestamp = DateTime.UtcNow,
                    Priority = 9
                },
                Headers = new QueueMessageHeaders
                {
                    {"header-key", "header-value"}
                }
            };



            GlobalTracer.Register(new Jaeger.Tracer.Builder("Byndyusoft.Net.RabbitMq").Build());

            var span = GlobalTracer.Instance.BuildSpan(nameof(Main)).StartActive(true);

            using var activity = ActivitySource.StartActivity(nameof(Main));
            activity?.AddBaggage("baggage-key1", "baggage-value1");
            activity?.AddBaggage("baggage-key2", "baggage-value2");

            await service.PublishBatchAsync(new[]{message, message2});

            service.Subscribe("queue", async (queueMessage, _) =>
            {
                Console.WriteLine(await queueMessage.Content.ReadAsStringAsync());
            });

            activity?.Dispose();
            span?.Dispose();

            Console.ReadKey();

            //await Task.Delay(5000);

            //while (true)
            //{
            //    var got = await service.GetAsync("queue2");
            //    if (got is null)
            //        break;

            //    Console.WriteLine(JsonConvert.SerializeObject(got));

            //    await service.AckAsync(got);
            //}
        }
    }
}