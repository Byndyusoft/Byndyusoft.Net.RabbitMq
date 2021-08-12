using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Consuming message chainable middleware
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IConsumeMiddleware<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Wraps message consuming chain
        /// </summary>
        /// <typeparam name="TMessage">Consuming message type</typeparam>
        /// <param name="message">Message</param>
        /// <param name="next">Next middleware in a chain</param>
        Task Handle(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next);
    }
}