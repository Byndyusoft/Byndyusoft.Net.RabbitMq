using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Service for publishing messages to bus
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        ///     Publishes new outgoing message into bus
        /// </summary>
        /// <typeparam name="TMessage">Outgoing message type</typeparam>
        /// <param name="message">Message itself</param>
        /// <param name="correlationId">Message unique key</param>
        /// <param name="headers">Additional message headers</param>
        /// <param name="returnedHandled">Returned messages handler</param>
        /// <param name="cancellationToken">Token for cancelling operation</param>
        Task Publish<TMessage>(
            TMessage message,
            string correlationId,
            Dictionary<string, string>? headers = null,
            Action<MessageReturnedEventArgs>? returnedHandled = null,
            CancellationToken cancellationToken = default) where TMessage : class;
    }
}