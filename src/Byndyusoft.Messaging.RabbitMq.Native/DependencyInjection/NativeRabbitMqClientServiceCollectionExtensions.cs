using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Native;
using Byndyusoft.Messaging.RabbitMq.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class NativeRabbitMqClientServiceCollectionExtensions
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