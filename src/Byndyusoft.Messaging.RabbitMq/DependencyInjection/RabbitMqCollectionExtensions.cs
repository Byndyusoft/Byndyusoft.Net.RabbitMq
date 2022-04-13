// ReSharper disable once CheckNamespace

using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Core;
using Byndyusoft.Messaging.RabbitMq.Core.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.InMemory;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqCollectionExtensions
    {
        public static IServiceCollection AddInMemoryRabbitMqClient(this IServiceCollection services)
        {
            Preconditions.CheckNotNull(services, nameof(services));

            AddRabbitMqCoreServices(services);

            services.AddSingleton<InMemoryRabbitMqClient>();
            services.AddSingleton<InMemoryRabbitMqClientHandler>();
            services.AddSingleton<IRabbitMqClientHandler, InMemoryRabbitMqClientHandler>();
            services.AddSingleton<IRabbitMqClient, InMemoryRabbitMqClient>();
            return services;
        }

        public static IServiceCollection AddInMemoryRabbitMqClient(this IServiceCollection services,
            Action<RabbitMqClientOptions> setupOptions)
        {
            services.Configure(setupOptions);

            return services.AddInMemoryRabbitMqClient();
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services,
            string connectionString)
        {
            return services.AddRabbitMqClient(options => { options.ConnectionString = connectionString; });
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services,
            Action<RabbitMqClientOptions> setupOptions)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(setupOptions, nameof(setupOptions));

            AddRabbitMqCoreServices(services);

            services.Configure(setupOptions);
            services.AddSingleton<IBusFactory, BusFactory>();
            services.AddSingleton<RabbitMqClient>();
            services.AddSingleton<IRabbitMqClient, RabbitMqClient>();
            services.AddSingleton<RabbitMqClientHandler>();
            services.AddSingleton<IRabbitMqClientHandler, RabbitMqClientHandler>();

            return services;
        }

        private static IServiceCollection AddRabbitMqCoreServices(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var clientOptions = sp.GetRequiredService<IOptions<RabbitMqClientOptions>>();
                return new RabbitMqClientActivitySource(clientOptions.Value.DiagnosticsOptions);
            });

            return services;
        }
    }
}