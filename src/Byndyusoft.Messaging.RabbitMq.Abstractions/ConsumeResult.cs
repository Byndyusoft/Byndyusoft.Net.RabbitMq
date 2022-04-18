using System;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class ConsumeResult
    {
        public static ConsumeResult Ack() => AckConsumeResult.Instance;
        public static ConsumeResult RejectWithRequeue() => RejectWithRequeueConsumeResult.Instance;
        public static ConsumeResult RejectWithoutRequeue() => RejectWithoutRequeueConsumeResult.Instance;
        public static ConsumeResult Retry() => RetryConsumeResult.Instance;
        public static ConsumeResult Error(Exception? e = null) => new ErrorConsumeResult(e);
    }

    public sealed class AckConsumeResult : ConsumeResult
    {
        public static readonly AckConsumeResult Instance = new AckConsumeResult();
    }

    public sealed class RejectWithRequeueConsumeResult : ConsumeResult
    {
        public static readonly RejectWithRequeueConsumeResult Instance = new RejectWithRequeueConsumeResult();
    }

    public sealed class RejectWithoutRequeueConsumeResult : ConsumeResult
    {
        public static readonly RejectWithoutRequeueConsumeResult Instance = new RejectWithoutRequeueConsumeResult();
    }

    public class RetryConsumeResult : ConsumeResult
    {
        public static readonly RetryConsumeResult Instance = new RetryConsumeResult();
    }

    public sealed class ErrorConsumeResult : ConsumeResult
    {
        public Exception? Exception { get; }

        public ErrorConsumeResult(Exception? exception)
        {
            Exception = exception;
        }
    }
}