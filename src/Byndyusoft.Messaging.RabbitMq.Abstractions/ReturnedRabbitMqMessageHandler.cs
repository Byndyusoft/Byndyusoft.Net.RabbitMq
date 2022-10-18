using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public delegate ValueTask ReturnedRabbitMqMessageHandler(ReturnedRabbitMqMessage message, CancellationToken cancellationToken);
}