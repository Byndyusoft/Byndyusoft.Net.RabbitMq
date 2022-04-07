using System;
using System.Collections.Generic;
using System.Linq;

namespace Byndyusoft.Messaging.RabbitMq.Utils
{
    public abstract class Disposable : IDisposable
    {
        internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            DisposeCore();
        }

        protected virtual void DisposeCore()
        {
        }

        public static void MultiDispose(IEnumerable<IDisposable> disposables)
        {
            var exceptions = new List<Exception>();

            foreach (var disposable in disposables)
                try
                {
                    disposable.Dispose();
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