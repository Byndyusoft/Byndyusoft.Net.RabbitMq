using System;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Extensions.Pipes;
using Byndyusoft.Net.RabbitMq.Extensions.Wrappers;
using Byndyusoft.Net.RabbitMq.Services;
using Jaeger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing;

namespace Byndyusoft.Net.RabbitMq.Tests
{
    public class Program
    {
        public static async Task Main()
        {
            var serviceCollection = new ServiceCollection();
            var tracer = new Tracer.Builder("Demo").Build();
            serviceCollection.AddSingleton<ITracer>(tracer)
                             .AddLogging(builder => builder.AddConsole())
                             .AddSingleton<TracerConsumeWrapper<RawDocument>>()
                             .AddSingleton<PushToErrorQueue<RawDocument>>()
                             .AddSingleton<TracerProduceWrapper<EnrichedDocument>>()
                             .AddSingleton<TraceReturned<EnrichedDocument>>()
                             .AddSingleton<IQueueService, QueueService>()
                             .AddSingleton<IBusFactory, BusFactory>();

            var serviceProvider =
            serviceCollection.AddRabbitMq(
                configurator => configurator.Connection("host=localhost")
                    .Exchange("incoming_documents",
                        exchangeConfigurator =>
                        {
                            exchangeConfigurator.Consume<RawDocument>("raw_documents", "raw")
                                .Wrap<TracerConsumeWrapper<RawDocument>>()
                                .PipeError<PushToErrorQueue<RawDocument>>();


                            exchangeConfigurator.Produce<EnrichedDocument>("enriched_documents", "enriched")
                                .Wrap<TracerProduceWrapper<EnrichedDocument>>()
                                .PipeReturned<TraceReturned<EnrichedDocument>>();
                        })).BuildServiceProvider();



            var queueService = serviceProvider.GetRequiredService<IQueueService>();
            await queueService.Initialize().ConfigureAwait(false);
            queueService.SubscribeAsync<RawDocument>(async raw =>
            {
                var enriched = new EnrichedDocument
                {
                    RawDocument = raw
                };

                await queueService.Publish(enriched, Guid.NewGuid().ToString());
            });
        }
    }
}
