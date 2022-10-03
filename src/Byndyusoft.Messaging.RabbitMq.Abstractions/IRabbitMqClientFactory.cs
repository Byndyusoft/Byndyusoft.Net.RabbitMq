namespace Byndyusoft.Messaging.RabbitMq
{
    /// <summary>
    ///     A factory abstraction for a component that can create <see cref="IRabbitMqClient" /> instances with custom
    ///     configuration for a given logical name.
    /// </summary>
    public interface IRabbitMqClientFactory
    {
        /// <summary>
        ///     Creates and configures a default  <see cref="IRabbitMqClient" /> instance.
        /// </summary>
        /// <returns>A new <see cref="IRabbitMqClient" /> instance.</returns>
        IRabbitMqClient CreateClient();

        /// <summary>
        ///     Creates and configures an <see cref="IRabbitMqClient" /> instance using the configuration that corresponds
        ///     to the logical name specified by <paramref name="name" />.
        /// </summary>
        /// <param name="name">The logical name of the client to create.</param>
        /// <returns>A new <see cref="IRabbitMqClient" /> instance.</returns>
        IRabbitMqClient CreateClient(string name);
    }
}