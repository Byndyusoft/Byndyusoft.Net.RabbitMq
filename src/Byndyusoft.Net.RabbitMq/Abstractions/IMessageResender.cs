using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Service for resending failed messages
    /// </summary>
    public interface IMessageResender
    {
        /// <summary>
        ///     Resends messages from particular error queue by message type
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="cancellationToken">Token for cancelling operation</param>
        Task ResendErrorMessages<TMessage>(CancellationToken cancellationToken = default);
    }
}