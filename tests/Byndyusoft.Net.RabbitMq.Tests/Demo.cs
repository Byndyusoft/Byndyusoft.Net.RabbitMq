using System;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Extensions.Middlewares;
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
            var firstServiceProvider = await InitFirstQueueService();
            var secondServiceProvider = await InitSecondQueueService();

            var firstQueueService = firstServiceProvider.GetRequiredService<IQueueService>();
            var firstTracer = firstServiceProvider.GetRequiredService<ITracer>();

            var secondQueueService = secondServiceProvider.GetRequiredService<IQueueService>();
            var secondTracer = secondServiceProvider.GetRequiredService<ITracer>();

            using var scope1 = firstTracer.BuildSpan(nameof(Main)).StartActive(true);
            using var scope2 = secondTracer.BuildSpan(nameof(Main)).StartActive(true);

            firstQueueService.SubscribeAsync<RawDocument>(async raw =>
            {
                Console.WriteLine("Consume raw");
                var enriched = new EnrichedDocument
                {
                    RawDocument = raw
                };

                Console.WriteLine("Push enriched");
                await firstQueueService.Publish(enriched, Guid.NewGuid().ToString());
            });

            secondQueueService.SubscribeAsync<EnrichedDocument>(raw =>
            {
                Console.WriteLine("Consume enriched");
                return Task.CompletedTask;
            });

            Console.WriteLine("Push enriched");
            await firstQueueService.Publish(new EnrichedDocument(), Guid.NewGuid().ToString()).ConfigureAwait(false);

            Console.WriteLine("Push raw");
            await secondQueueService.Publish(new RawDocument {  Int = 100500 }, Guid.NewGuid().ToString());

            Console.WriteLine("press any key...");
            Console.ReadKey();
            Console.WriteLine("Bye");
        }

        private static async Task<IServiceProvider> InitFirstQueueService()
        {
            var serviceCollection = BuildServiceCollection();

            var serviceProvider =
                serviceCollection.AddRabbitMq(
                    configurator => configurator.Connection("host=localhost")
                        .InjectServices(register => { })
                        .Exchange("incoming_documents",
                            exchangeConfigurator =>
                            {
                                exchangeConfigurator.Consume<RawDocument>("raw_documents", "raw")
                                    .Wrap<TracerConsumeMiddleware<RawDocument>>();


                                exchangeConfigurator.Produce<EnrichedDocument>("enriched_documents", "enriched")
                                    .Wrap<TracerProduceMiddleware<EnrichedDocument>>()
                                    .WrapReturned<TraceReturnedMiddleware<EnrichedDocument>>();

                            })).BuildServiceProvider();



            var queueService = serviceProvider.GetRequiredService<IQueueService>();
            await queueService.Initialize().ConfigureAwait(false);
            return serviceProvider;
        }

        private static async Task<IServiceProvider> InitSecondQueueService()
        {
            var serviceCollection = BuildServiceCollection();

            var serviceProvider =
                serviceCollection.AddRabbitMq(
                    configurator => configurator.Connection("host=localhost")
                        .InjectServices(register => { })
                        .Exchange("incoming_documents",
                            exchangeConfigurator =>
                            {
                                exchangeConfigurator.Produce<RawDocument>("raw_documents", "raw")
                                                    .WrapReturned<TraceReturnedMiddleware<RawDocument>>();

                                exchangeConfigurator.Consume<EnrichedDocument>("enriched_documents", "enriched");

                            })).BuildServiceProvider();



            var queueService = serviceProvider.GetRequiredService<IQueueService>();
            await queueService.Initialize().ConfigureAwait(false);
            return serviceProvider;
        }

        private static ServiceCollection BuildServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var tracer = new Tracer.Builder("Demo").Build();
            serviceCollection.AddSingleton<ITracer>(tracer)
                .AddLogging(builder => builder.AddConsole())
                .AddSingleton<IConsumeMiddleware<RawDocument>, TracerConsumeMiddleware<RawDocument>>()
                .AddSingleton<IProduceMiddleware<RawDocument>, TracerProduceMiddleware<RawDocument>>()
                .AddSingleton<IConsumeMiddleware<EnrichedDocument>, TracerConsumeMiddleware<EnrichedDocument>>()
                .AddSingleton<IProduceMiddleware<EnrichedDocument>, TracerProduceMiddleware<EnrichedDocument>>()
                .AddSingleton<IReturnedMiddleware<EnrichedDocument>, TraceReturnedMiddleware<EnrichedDocument>>()
                .AddSingleton<IReturnedMiddleware<RawDocument>, TraceReturnedMiddleware<RawDocument>>()
                .AddSingleton<IQueueService, QueueService>()
                .AddSingleton<IBusFactory, BusFactory>();
            return serviceCollection;
        }
    }
}
