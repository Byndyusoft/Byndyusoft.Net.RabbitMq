using System;
using Byndyusoft.Messaging.RabbitMq.Rpc;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionRcpExtensions
    {
        public static IServiceCollection AddRpcService<TRpcService>(this IServiceCollection services)
            where TRpcService : class, IRpcService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRpcService, TRpcService>());
            services.AddSingleton<TRpcService>();

            return services;
        }

        public static IServiceCollection AddRabbitMqRpc(this IServiceCollection services)
        {
            return AddRabbitMqRpc(services, _ => { });
        }

        public static IServiceCollection AddRabbitMqRpc(
            this IServiceCollection services,
            Action<RabbitMqRpcOptions> configure)
        {
            services.AddOptions();
            services.Configure(configure);

            return services.AddHostedService<RpcHostedService>();
        }
    }
}