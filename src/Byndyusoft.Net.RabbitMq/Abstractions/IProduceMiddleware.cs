using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Producing message chainable middleware
    /// </summary>
    /// <typeparam name="TMessage">Producing message type</typeparam>
    public interface IProduceMiddleware<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Wraps message producing chain
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="next">Next middleware in a chain</param>
        Task Handle(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next);
    }
}