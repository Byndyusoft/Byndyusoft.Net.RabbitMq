using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqClientServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services,
            Action<RabbitMqClientOptions> setupOptions)
        {
            Preconditions.CheckNotNull(services, nameof(services));

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

            services.AddSingleton<IBusFactory, BusFactory>();
            services.AddSingleton<RabbitMqClient>();
            services.AddSingleton<IRabbitMqClient, RabbitMqClient>();
            services.AddSingleton<IRabbitMqClientFactory, RabbitMqClientFactory>();
            services.AddSingleton<RabbitMqClientHandler>();
            services.AddSingleton<IRabbitMqClientHandler, RabbitMqClientHandler>();
            services.AddSingleton<IRabbitMqClientHandlerFactory, RabbitMqClientHandlerFactory>();

            return services;
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services, string connectionString)
        {
            Preconditions.CheckNotNull(services, nameof(services));

            return services.AddRabbitMqClient(
                options => { options.ConnectionString = connectionString; });
        }

        public static IServiceCollection AddRabbitMqClient(this IServiceCollection services, string name,
            string connectionString)
        {
            Preconditions.CheckNotNull(services, nameof(services));

            return services.AddRabbitMqClient(name,
                options => { options.ConnectionString = connectionString; });
        }
    }
}