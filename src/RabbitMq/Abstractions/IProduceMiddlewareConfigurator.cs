namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for building pipeline for producing message
    /// </summary>
    /// <typeparam name="TMessage">Outgoing message type</typeparam>
    public interface IProduceMiddlewareConfigurator<TMessage> : IProduceReturnedMiddlewareConfigurator<TMessage>
        where TMessage : class
    {
        /// <summary>
        ///     Adds middleware to producing pipeline
        /// </summary>
        /// <typeparam name="TMiddleware">Middleware type</typeparam>
        IProduceMiddlewareConfigurator<TMessage> Wrap<TMiddleware>() where TMiddleware : IProduceMiddleware<TMessage>;
    }
}