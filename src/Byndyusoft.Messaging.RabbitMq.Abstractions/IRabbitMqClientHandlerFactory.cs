namespace Byndyusoft.Messaging.RabbitMq
{
    /// <summary>
    ///     A factory abstraction for a component that can create <see cref="RabbitMqClientHandler" /> instances with custom
    ///     configuration for a given logical name.
    /// </summary>
    public interface IRabbitMqClientHandlerFactory
    {
        /// <summary>
        ///     Creates and configures a <see cref="RabbitMqClientHandler" /> instance.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>A new <see cref="RabbitMqClientHandler" /> instance.</returns>
        IRabbitMqClientHandler CreateHandler(RabbitMqClientOptions options);
    }
}