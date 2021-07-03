using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Net.RabbitMq.Extensions
{

    /// <summary>
    ///     Расширения для внедерния RabbitMq
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Добавляет инфраструктуру работы с очередями RabbitMq в зависимости
        /// </summary>
        /// <param name="services">Коллекция зависимостей</param>
        /// <param name="setup">Делегат для конфигации очередей</param>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection services,
            Action<IConnectionConfigurator> setup)
        {
            var configurator = new ConnectionConfigurator();
            setup(configurator);
            var configuration = configurator.Build();
            services.AddSingleton(configuration);
            services.AddSingleton<IQueueService, QueueService>();
            return services;
        }
    }
}
