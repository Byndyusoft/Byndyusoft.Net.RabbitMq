namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for building Exchange configuration
    /// </summary>
    public interface IExchangeConfigurator
    {
        /// <summary>
        ///     Adds pipeline for consuming message
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="routingKey">Queue routing key</param>
        IConsumeMiddlewareConfigurator<TMessage> Consume<TMessage>(string queueName, string routingKey)
            where TMessage : class;

        /// <summary>
        ///     Adds pipeline for consuming message
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        IConsumeMiddlewareConfigurator<TMessage> Consume<TMessage>() where TMessage : class;

        /// <summary>
        ///     Adds pipeline for producing message
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="routingKey">Queue routing key</param>
        IProduceMiddlewareConfigurator<TMessage> Produce<TMessage>(string queueName, string routingKey)
            where TMessage : class;

        /// <summary>
        ///     Adds pipeline for producing message
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        IProduceMiddlewareConfigurator<TMessage> Produce<TMessage>() where TMessage : class;
    }
}