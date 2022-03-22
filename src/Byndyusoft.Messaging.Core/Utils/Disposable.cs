using System;

namespace Byndyusoft.Messaging.Utils
{
    public abstract class Disposable : IDisposable
    {
        internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Disposable()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}