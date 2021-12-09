using System;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for building topology configuration
    /// </summary>
    public interface ITopologyConfigurator
    {
        /// <summary>
        ///     Add another exchange to topology
        /// </summary>
        /// <param name="exchangeName">Exchange name</param>
        /// <param name="setupExchange">Exchange set up delegate</param>
        ITopologyConfigurator Exchange(string exchangeName, Action<IExchangeConfigurator> setupExchange);
    }
}