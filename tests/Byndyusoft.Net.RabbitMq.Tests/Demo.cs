using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Extensions;
using Byndyusoft.Net.RabbitMq.Services;
using Byndyusoft.Net.RabbitMq.Services.Pipes;
using Byndyusoft.Net.RabbitMq.Services.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Net.RabbitMq.Tests
{
    public class Program
    {
        public static async Task Main()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<TracerConsumeWrapper<RawDocument>>()
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

                await queueService.Publish(enriched);
            });
        }
    }
}
