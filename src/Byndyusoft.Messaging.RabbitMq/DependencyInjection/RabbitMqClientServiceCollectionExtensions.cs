using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqClientServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services,
            Action<RabbitMqClientOptions> setupOptions)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(setupOptions, nameof(setupOptions));

            return services.AddRabbitMqClient(Options.Options.DefaultName, setupOptions);
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services,
            string name,
            Action<RabbitMqClientOptions> setupOptions)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(name, nameof(name));
            Preconditions.CheckNotNull(setupOptions, nameof(setupOptions));

            services.AddOptions();
            services.Configure(name, setupOptions);

            services.TryAddSingleton<IBusFactory, BusFactory>();
            services.TryAddSingleton<RabbitMqClient>();
            services.TryAddSingleton<IRabbitMqClient, RabbitMqClient>();
            services.TryAddSingleton<IRabbitMqClientFactory, RabbitMqClientFactory>();
            services.TryAddSingleton<RabbitMqClientHandler>();
            services.TryAddSingleton<IRabbitMqClientHandler, RabbitMqClientHandler>();
            services.TryAddSingleton<IRabbitMqClientHandlerFactory, RabbitMqClientHandlerFactory>();

            return services;
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services, string connectionString)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));

            return services.AddRabbitMqClient(Options.Options.DefaultName, connectionString);
        }

        public static IServiceCollection AddRabbitMqClient(
            this IServiceCollection services,
            string name,
            string connectionString)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(name, nameof(name));
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));

            return services.AddRabbitMqClient(name,
                options => { options.ConnectionString = connectionString; });
        }
    }
}