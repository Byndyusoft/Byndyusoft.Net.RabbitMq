using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.Abstractions
{
    public interface IQueueMessageHandler
    {
        Task<ConsumeResult> HandleAsync(ConsumedQueueMessage message, CancellationToken cancellationToken);
    }
}