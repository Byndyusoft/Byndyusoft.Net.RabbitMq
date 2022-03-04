namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for building pipeline for returned produced messages
    /// </summary>
    /// <typeparam name="TMessage">Outgoing message type</typeparam>
    public interface IProduceReturnedMiddlewareConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Adds middleware to returned message pipeline
        /// </summary>
        /// <typeparam name="TMiddleware">Middleware type</typeparam>
        IProduceReturnedMiddlewareConfigurator<TMessage> WrapReturned<TMiddleware>()
            where TMiddleware : IReturnedMiddleware<TMessage>;
    }
}