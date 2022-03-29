using System;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.Utils;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitQueueServiceCollectionExtensions
    {
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