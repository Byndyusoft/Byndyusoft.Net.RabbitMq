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
        private readonly RabbitMqClientCoreOptions _options;
        private readonly string _rpcQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ReceivedRabbitMqMessage>> _rpcCalls = new();
        private IDisposable? _rpcQueueConsumer;
        private Timer? _timer;
        private long _lastCallTimeBinary; 

        public RabbitMqRpcClient(IRabbitMqClientHandler handler, RabbitMqClientCoreOptions options)
        {
            _handler = handler;
            _options = options;
            _rpcQueueName = options.GetRpcReplyQueueName();
        }

        public async Task<ReceivedRabbitMqMessage> MakeRpc(
            RabbitMqMessage message,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            Interlocked.Exchange(ref _lastCallTimeBinary, DateTime.UtcNow.ToBinary());

            await StartRpc(cancellationToken)
                .ConfigureAwait(false);

            var correlationId = message.Properties.CorrelationId ??= Guid.NewGuid().ToString();
            message.Properties.ReplyTo = _rpcQueueName;

            var tcs = new TaskCompletionSource<ReceivedRabbitMqMessage>();

            cancellationToken.Register(OnCancelled, correlationId);

            await _handler.PublishMessageAsync(message, cancellationToken)
                .ConfigureAwait(false);

            _rpcCalls.AddOrUpdate(correlationId, tcs, (_, _) => tcs);

            return await tcs.Task
                .ConfigureAwait(false);
        }

        public IRabbitMqConsumer SubscribeRpc(RabbitMqClientCore coreClient, string queueName, RabbitMqRpcHandler onMessage)
        {
            async Task<ConsumeResult> OnRpcCall(ReceivedRabbitMqMessage requestMessage, CancellationToken cancellationToken)
            {
                var replyTo = requestMessage.Properties.ReplyTo;
                if (replyTo is null)
                    return ConsumeResult.Error("RPC message must have ReplyTo property");

                var correlationId = requestMessage.Properties.CorrelationId;
                if (correlationId is null)
                    return ConsumeResult.Error("RPC message must have CorrelationId property");

                RpcResult rpcResult;
                try
                {
                    rpcResult = await onMessage(requestMessage, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    rpcResult = RpcResult.Error(e);
                }

                var responseMessage =
                    RabbitMqMessageFactory.CreateRpcResponseMessage(requestMessage, rpcResult);
                await _handler.PublishMessageAsync(responseMessage, cancellationToken)
                    .ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return new RabbitMqConsumer(coreClient, queueName, OnRpcCall);
        }

        private void OnCancelled(object state)
        {
            var correlationId = (string) state;
            if (_rpcCalls.TryRemove(correlationId, out var tcs) == false)
                return;

            tcs.SetCanceled();
        }

        private Task<HandlerConsumeResult> OnReply(ReceivedRabbitMqMessage message, CancellationToken _)
        {
            Interlocked.Exchange(ref _lastCallTimeBinary, DateTime.UtcNow.ToBinary());

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

            _timer?.Dispose();
            _timer = null!;

            _mutex?.Dispose();
            _mutex = null!;

            _rpcCalls.Values.ToList().ForEach(tcs => tcs.SetCanceled());
            _rpcCalls.Clear();
        }

        private async Task StartRpc(CancellationToken cancellationToken)
        {
            if (IsRpcStarted == false)
            {
                await _mutex!.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    if (IsRpcStarted == false)
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
                                    null,
                                    OnReply,
                                    cancellationToken)
                                .ConfigureAwait(false);
 
                        _timer = new Timer(OnTick, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                    }
                }
                finally
                {
                    _mutex.Release();
                }
            }
        }

        private bool IsRpcStarted => _rpcQueueConsumer is not null;

        private DateTime LastCallTime => DateTime.FromBinary(_lastCallTimeBinary);

        private async Task StopRpc(bool force, CancellationToken cancellationToken)
        {
            await _mutex!.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (force == false && _rpcCalls.Any())
                    return;

                _rpcQueueConsumer?.Dispose();
                _rpcQueueConsumer = null;

                _timer?.Dispose();
                _timer = null!;

                _rpcCalls.Values.ToList().ForEach(tcs => tcs.SetCanceled());
                _rpcCalls.Clear();
            }
            finally
            {
                _mutex.Release();
            }
        }
        
        private async void OnTick(object state)
        {
            var cancellationToken = CancellationToken.None;

            try
            {
                var rcpQueueExists = await _handler.QueueExistsAsync(_rpcQueueName, cancellationToken)
                    .ConfigureAwait(false);
                if (rcpQueueExists == false)
                {
                    await StopRpc(true, cancellationToken)
                        .ConfigureAwait(false);
                }

                var isIdle = DateTime.UtcNow.Subtract(LastCallTime) > _options.RpcIdleLifetime;
                if (isIdle)
                {
                    await StopRpc(false, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}