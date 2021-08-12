namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///    Api for builing pipeline for producing message
    /// </summary>
    /// <typeparam name="TMessage">Incoming message type</typeparam>
    public interface IProduceMiddlewareConfigurator<TMessage> : IProduceReturnedPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Adds middleware to producing pipeline
        /// </summary>
        /// <typeparam name="TMiddleware">Middleware type</typeparam>
        IProduceMiddlewareConfigurator<TMessage> Wrap<TMiddleware>() where TMiddleware : IProduceMiddleware<TMessage>;
    }
}
