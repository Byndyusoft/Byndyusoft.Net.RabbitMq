using System;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.InMemory;
using Byndyusoft.Messaging.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitQueueServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryRabbitQueueService(this IServiceCollection services)
        {
            services.AddSingleton<IRabbitQueueServiceHandler, InMemoryRabbitQueueServiceHandler>();
            services.AddSingleton<IRabbitQueueService, RabbitQueueService>();
            return services;
        }

        public static IServiceCollection AddInMemoryRabbitQueueService(this IServiceCollection services,
            Action<QueueServiceOptions> setupOptions)
        {
            services.Configure(setupOptions);

            return services.AddInMemoryRabbitQueueService();
        }

        public static IServiceCollection AddRabbitQueueService(this IServiceCollection services,
            string connectionString)
        {
            return services.AddRabbitQueueService(options => { options.ConnectionString = connectionString; });
        }

        public static IServiceCollection AddRabbitQueueService(this IServiceCollection services,
            Action<QueueServiceOptions> setupOptions)
        {
            Preconditions.CheckNotNull(services, nameof(services));
            Preconditions.CheckNotNull(setupOptions, nameof(setupOptions));

            services.Configure(setupOptions);
            services.AddSingleton<IBusFactory, BusFactory>();
            services.AddSingleton<IQueueService, RabbitQueueService>();
            services.AddSingleton<IRabbitQueueService, RabbitQueueService>();
            services.AddSingleton<IRabbitQueueServiceHandler, RabbitQueueServiceHandler>();

            return services;
        }
    }
}