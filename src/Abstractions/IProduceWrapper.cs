using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Producing message chainable middleware
    /// </summary>
    /// <typeparam name="TMessage">Producing message type</typeparam>
    public interface IProduceWrapper<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Wraps message producing chain
        /// </summary>
        /// <typeparam name="TMessage">Producing message type</typeparam>
        /// <param name="message">Message</param>
        /// <param name="next">Next middleware in a chain</param>
        Task WrapPipe(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next);
    }
}