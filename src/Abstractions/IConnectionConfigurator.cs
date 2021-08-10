namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for building Rabbit connection and topology configuration
    /// </summary>
    public interface IConnectionConfigurator
    {
        /// <summary>
        ///     Sets up conections string to Rabbit
        /// </summary>
        /// <returns>
        ///     Returns topology configuration api
        /// </returns>
        ITopologyConfigurator Connection(string connectionString);
    }
}
