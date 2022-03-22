using Byndyusoft.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Messaging.RabbitMq.DependencyInjection
{
    public static class RabbitQueueServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitQueueService(this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IQueueService, RabbitQueueService>();

            return services;
        }
    }
}