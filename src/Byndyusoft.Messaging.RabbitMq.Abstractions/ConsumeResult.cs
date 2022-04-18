using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class ConsumeResult
    {
        public static ConsumeResult Ack()
        {
            return AckConsumeResult.Instance;
        }

        public static ConsumeResult RejectWithRequeue()
        {
            return RejectWithRequeueConsumeResult.Instance;
        }

        public static ConsumeResult RejectWithoutRequeue()
        {
            return RejectWithoutRequeueConsumeResult.Instance;
        }

        public static ConsumeResult Retry()
        {
            return RetryConsumeResult.Instance;
        }

        public static ConsumeResult Error(Exception? e = null)
        {
            return new ErrorConsumeResult(e);
        }
    }

    public sealed class AckConsumeResult : ConsumeResult
    {
        public static readonly AckConsumeResult Instance = new();
    }

    public sealed class RejectWithRequeueConsumeResult : ConsumeResult
    {
        public static readonly RejectWithRequeueConsumeResult Instance = new();
    }

    public sealed class RejectWithoutRequeueConsumeResult : ConsumeResult
    {
        public static readonly RejectWithoutRequeueConsumeResult Instance = new();
    }

    public class RetryConsumeResult : ConsumeResult
    {
        public static readonly RetryConsumeResult Instance = new();
    }

    public sealed class ErrorConsumeResult : ConsumeResult
    {
        public ErrorConsumeResult(Exception? exception)
        {
            Exception = exception;
        }

        public Exception? Exception { get; }
    }
}