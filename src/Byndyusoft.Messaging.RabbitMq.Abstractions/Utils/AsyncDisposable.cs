using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq.Utils
{
    public abstract class AsyncDisposable : Disposable, IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;

            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(false);
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            return new();
        }

        public static async ValueTask MultiDispose(IEnumerable<IAsyncDisposable> disposables)
        {
            var exceptions = new List<Exception>();

            foreach (var disposable in disposables)
                try
                {
                    await disposable.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }
}