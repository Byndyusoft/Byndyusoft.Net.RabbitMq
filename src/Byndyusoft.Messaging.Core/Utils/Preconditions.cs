using System;

namespace Byndyusoft.Messaging.Utils
{
    public static class Preconditions
    {
        public static void CheckNotNull<T>(T value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
        }

        public static void CheckNotNull<T>(T value, string paramName, string message)
        {
            if (value is null)
                throw new ArgumentNullException(paramName, message);
        }

        public static void CheckNotDisposed(Disposable disposable)
        {
            if (disposable.IsDisposed)
                throw new ObjectDisposedException(disposable.GetType().Name);
        }

        public static void Check(bool condition, string message)
        {
            if (condition == false)
                throw new InvalidOperationException(message);
        }
    }
}