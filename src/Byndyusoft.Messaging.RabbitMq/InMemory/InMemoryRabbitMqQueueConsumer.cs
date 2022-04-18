using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    internal class InMemoryRabbitMqQueueConsumer : Disposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> _onMessage;
        private readonly InMemoryRabbitMqQueue _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _timer;

        public InMemoryRabbitMqQueueConsumer(InMemoryRabbitMqQueue queue,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> onMessage,
            int prefetchCount)
        {
            _queue = queue;
            _onMessage = onMessage;
            _timer = new Timer(DoWork, null, 0, 1000);
            _semaphore = new SemaphoreSlim(prefetchCount);
            Tag = Guid.NewGuid().ToString();
        }

        public string Tag { get; }

        private void DoWork(object? state)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            while (_semaphore.Wait(TimeSpan.Zero))
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var message = _queue.Get(Tag);
                    if (message is null)
                    {
                        _semaphore.Release();
                        break;
                    }

                    var _ = Task.Run(async () =>
                        {
                            await HandleMessage(message, cancellationToken);
                            _semaphore.Release();
                        },
                        cancellationToken);
                }
                catch
                {
                    _semaphore.Release();
                    throw;
                }
        }

        private async Task HandleMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var consumeResult = await _onMessage(message, cancellationToken)
                    .ConfigureAwait(false);
                if (consumeResult == HandlerConsumeResult.Ack)
                    _queue.Ack(message);
                else if (consumeResult == HandlerConsumeResult.RejectWithRequeue)
                    _queue.Reject(message, true);
                else if (consumeResult == HandlerConsumeResult.RejectWithoutRequeue)
                    _queue.Reject(message, false);
                else throw new InMemoryRabbitMqException($"Unsupported consume result: {consumeResult}");
            }
            catch
            {
                _queue.Reject(message, true);
            }
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();

            _timer.Dispose();
            _cancellationTokenSource.Dispose();
            _semaphore.Dispose();
            _queue.Consumers.Remove(this);
        }
    }
}