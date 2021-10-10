using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Services;
using Byndyusoft.Net.RabbitMq.Services.Configuration;
using Byndyusoft.Net.RabbitMq.Services.Imitation;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    ///     Dependency injection of package types
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds RabbitMq infrastructure to DI
        /// </summary>
        /// <param name="services">DI service collection</param>
        /// <param name="setup">Rabbit connection and pipelines configuration</param>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection services,
            Action<IConnectionConfigurator> setup)
        {
            var configurator = new ConnectionConfigurator();
            setup(configurator);
            var configuration = configurator.Build();
            services.AddSingleton(configuration);
            services.AddSingleton<IQueueService, QueueService>();
            services.AddSingleton<IBusFactory, BusFactory>();
            return services;
        }

        /// <summary>
        ///     Adds RabbitMq test infrastructure to DI
        /// </summary>
        /// <param name="services">DI service collection</param>
        public static IServiceCollection AddRabbitMqMockLayer(
            this IServiceCollection services)
        {
            var mockLayer = new QueueMockLayer();
            services.AddSingleton<IQueueMockLayer>(mockLayer);
            services.AddSingleton(mockLayer.BusFactory);
            return services;
        }
    }
}
