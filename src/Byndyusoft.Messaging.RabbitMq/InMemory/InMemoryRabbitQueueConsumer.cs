using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    internal class InMemoryRabbitQueueConsumer : Disposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> _onMessage;
        private readonly InMemoryRabbitQueue _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _timer;

        public InMemoryRabbitQueueConsumer(InMemoryRabbitQueue queue,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage, int prefetchCount)
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
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var message = _queue.Get(Tag);
                    if (message is null)
                    {
                        _semaphore.Release();
                        break;
                    }

                    var _ = Task.Run(async () =>
                    {
                        try
                        {
                            var consumeResult = await _onMessage(message, cancellationToken)
                                .ConfigureAwait(false);
                            if (consumeResult == ConsumeResult.Ack)
                                _queue.Ack(message);
                            else if (consumeResult == ConsumeResult.RejectWithRequeue)
                                _queue.Reject(message, true);
                            else if (consumeResult == ConsumeResult.RejectWithoutRequeue)
                                _queue.Reject(message, false);
                            else throw new InMemoryRabbitException($"Unsupported consume result: {consumeResult}");
                        }
                        catch
                        {
                            _queue.Reject(message, true);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, cancellationToken);
                }
                catch
                {
                    _semaphore.Release();
                    throw;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _timer.Dispose();
                _cancellationTokenSource.Dispose();
                _semaphore.Dispose();
                _queue.Consumers.Remove(this);
            }
        }
    }
}