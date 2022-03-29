using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitQueueServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitQueueService(this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IBusFactory, BusFactory>();
            services.AddSingleton<IQueueService, RabbitQueueService>();
            services.AddSingleton<IRabbitQueueService, RabbitQueueService>();
            services.AddTransient<IRabbitQueueServiceHandler>(sp =>
                new RabbitQueueServiceHandler(connectionString, sp.GetService<IBusFactory>()));

            return services;
        }
    }
}