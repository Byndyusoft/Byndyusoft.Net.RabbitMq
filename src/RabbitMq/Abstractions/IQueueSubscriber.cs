using System;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Service for setting up subscription on messages from bus
    /// </summary>
    public interface IQueueSubscriber
    {
        /// <summary>
        ///     Subscribes on incoming messages from bus
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="processMessage">Incoming message handler</param>
        /// <param name="cancellationToken">Token for cancelling operation</param>
        public void Subscribe<TMessage>(Func<TMessage, Task> processMessage,
            CancellationToken cancellationToken = default) where TMessage : class;
    }
}