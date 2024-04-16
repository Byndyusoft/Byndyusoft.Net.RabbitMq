namespace Byndyusoft.Messaging.RabbitMq
{
    /// <summary>
    ///     A factory abstraction for a component that can create <see cref="RabbitMqClientHandler" /> instances with custom
    ///     configuration for a given logical name.
    /// </summary>
    public interface IRabbitMqClientHandlerFactory
    {
        /// <summary>
        ///     Creates and configures a <see cref="RabbitMqClientHandler" /> instance using the configuration that corresponds
        ///     to the logical name specified by <paramref name="name" />.
        /// </summary>
        /// <param name="name">The logical name of the message handler to create.</param>
        /// <param name="options"></param>
        /// <returns>A new <see cref="RabbitMqClientHandler" /> instance.</returns>
        RabbitMqClientHandler CreateHandler(string name, RabbitMqClientOptions options);
    }
}