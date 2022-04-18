using System;
using System.Diagnostics;

namespace Byndyusoft.Messaging.RabbitMq.Utils
{
    [DebuggerStepThrough]
    public static class Preconditions
    {
        public static T CheckNotNull<T>(T? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
            return value;
        }

        public static T CheckNotNull<T>(T? value, string paramName, string message)
        {
            if (value is null)
                throw new ArgumentNullException(paramName, message);
            return value;
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

        public static int? CheckNotNegative(int? value, string paramName)
        {
            return value switch
            {
                null => value,
                <= 0 => throw new ArgumentOutOfRangeException(paramName),
                _ => value
            };
        }
    }
}