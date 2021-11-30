using System;
using EasyNetQ.DI;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for injecting custom services into EasyNetQ
    /// </summary>
    public interface IInjectionConfigurator : ITopologyConfigurator
    {
        /// <summary>
        ///     Adds custom registrations into EasyNetQ
        /// </summary>
        /// <returns>
        ///     Returns topology configuration api
        /// </returns>
        ITopologyConfigurator InjectServices(Action<IServiceRegister> register);
    }
}