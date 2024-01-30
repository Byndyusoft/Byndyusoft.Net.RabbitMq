using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
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

            return services.AddInMemoryRabbitMqClient(_ => { });
        }

        public static IServiceCollection AddInMemoryRabbitMqClient(this IServiceCollection services,
            Action<RabbitMqClientCoreOptions> setupOptions)
        {
            services.Configure(setupOptions);

            services.AddSingleton(_ => new RabbitMqClientActivitySource());

            services.TryAddSingleton<InMemoryRabbitMqClient>();
            services.TryAddSingleton<InMemoryRabbitMqClientHandler>();
            services.TryAddSingleton<IRabbitMqClientHandler, InMemoryRabbitMqClientHandler>();
            services.TryAddSingleton<IRabbitMqClient, InMemoryRabbitMqClient>();
            return services;
        }
    }
}