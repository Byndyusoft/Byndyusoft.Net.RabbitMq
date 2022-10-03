using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal class PulledRabbitMqMessage : ReceivedRabbitMqMessage
    {
        private readonly IRabbitMqClientHandler _handler;

        public PulledRabbitMqMessage(IRabbitMqClientHandler handler)
        {
            _handler = Preconditions.CheckNotNull(handler, nameof(handler));
        }

        public bool IsCompleted { get; set; }

        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);

            await RejectAsync().ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            RejectAsync()
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async ValueTask RejectAsync()
        {
            if (IsCompleted) return;

            await _handler.RejectMessageAsync(this, true, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}