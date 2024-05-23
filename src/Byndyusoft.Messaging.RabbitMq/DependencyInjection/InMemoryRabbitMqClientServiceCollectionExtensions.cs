using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.InMemory;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemoryRabbitMqClientServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryRabbitMqClient(this IServiceCollection services)
        {
            Preconditions.CheckNotNull(services, nameof(services));

            services.AddRabbitMqClientFactory();

            services.TryAddSingleton<IRabbitMqClientHandlerFactory, InMemoryRabbitMqClientHandlerFactory>();

            return services;
        }
    }
}