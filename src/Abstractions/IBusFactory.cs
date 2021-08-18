using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Rabbit connections factory
    /// </summary>
    public interface IBusFactory
    {
        /// <summary>
        ///     Returns new connection to Rabbit
        /// </summary>
        /// <param name="configuration">Configuration of Bus</param>
        IBus CreateBus(RabbitMqConfiguration configuration);
    }
}