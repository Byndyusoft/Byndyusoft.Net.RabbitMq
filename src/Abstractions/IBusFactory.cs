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
        /// <param name="connectionString">Connection string (for example 'host=localhost')</param>
        IBus CreateBus(string connectionString);
    }
}