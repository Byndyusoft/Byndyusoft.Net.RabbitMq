using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Messages;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    internal class RabbitMqRpcClient : Disposable
    {
        private SemaphoreSlim? _mutex = new(1, 1);
        private readonly IRabbitMqClientHandler _handler;
        private bool _isRpcStarted = false;
        private readonly string _rpcQueueName = Guid.NewGuid().ToString();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ReceivedRabbitMqMessage>> _rpcCalls = new();
        private IDisposable? _rpcQueueConsumer;

        public RabbitMqRpcClient(IRabbitMqClientHandler handler)
        {
            _handler = handler;
        }

        public async Task<ReceivedRabbitMqMessage> Rpc(
            RabbitMqMessage message,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            await StartRpc(cancellationToken).ConfigureAwait(false);

            var correlationId = message.Properties.CorrelationId ??= Guid.NewGuid().ToString();
            message.Properties.ReplyTo = _rpcQueueName;

            var tcs = new TaskCompletionSource<ReceivedRabbitMqMessage>();

            cancellationToken.Register(OnCancelled, correlationId);

            _rpcCalls.AddOrUpdate(correlationId, tcs, (_, _) => tcs);

            await _handler.PublishMessageAsync(message, cancellationToken)
                .ConfigureAwait(false);

            return await tcs.Task;
        }

        private void OnCancelled(object state)
        {
            var correlationId = (string) state;
            if (_rpcCalls.TryRemove(correlationId, out var tcs) == false)
                return;

            tcs.SetCanceled();
        }

        private Task<HandlerConsumeResult> OnMessage(ReceivedRabbitMqMessage message,
            CancellationToken cancellationToken)
        {
            var correlationId = message.Properties.CorrelationId;
            if (correlationId is not null &&
                _rpcCalls.TryRemove(correlationId, out var tcs))
            {
                var exception = message.Headers.GetException();
                if (exception is not null)
                    tcs.SetException(exception);
                tcs.SetResult(message);
            }

            return Task.FromResult(HandlerConsumeResult.Ack);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            _rpcQueueConsumer?.Dispose();
            _rpcQueueConsumer = null!;

            _mutex?.Dispose();
            _mutex = null!;

            _rpcCalls.Values.ToList().ForEach(tcs => tcs.SetCanceled());
            _rpcCalls.Clear();
        }

        private async Task StartRpc(CancellationToken cancellationToken)
        {
            if (_isRpcStarted == false)
            {
                await _mutex!.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    if (_isRpcStarted == false)
                    {
                        await _handler.CreateQueueAsync(_rpcQueueName,
                                QueueOptions.Default
                                    .AsExclusive(true)
                                    .AsAutoDelete(true),
                                cancellationToken)
                            .ConfigureAwait(false);
                        _rpcQueueConsumer =
                            await _handler.StartConsumeAsync(_rpcQueueName,
                                    true,
                                    1,
                                    OnMessage,
                                    cancellationToken)
                                .ConfigureAwait(false);
                    }
                }
                finally
                {
                    _mutex.Release();
                }
            }
        }
    }
}