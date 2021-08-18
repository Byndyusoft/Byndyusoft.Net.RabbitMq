using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Returned message chainable middleware
    /// </summary>
    /// <typeparam name="TMessage">Returned message type</typeparam>
    public interface IReturnedMiddleware<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Wraps returned message consuming chain
        /// </summary>
        /// <param name="args">Message</param>
        /// <param name="next">Next middleware in a chain</param>
        Task Wrap(MessageReturnedEventArgs args, Func<MessageReturnedEventArgs, Task> next);
    }
}