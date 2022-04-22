using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class ConsumeResult
    {
        public static readonly ConsumeResult Ack = new AckConsumeResult();

        public static readonly ConsumeResult RejectWithRequeue = new RejectWithRequeueConsumeResult();

        public static readonly ConsumeResult RejectWithoutRequeue = new RejectWithoutRequeueConsumeResult();

        public static readonly ConsumeResult Retry = new RetryConsumeResult();

        public static ConsumeResult Error(Exception? e = null)
        {
            return new ErrorConsumeResult(e);
        }
    }

    public sealed class AckConsumeResult : ConsumeResult
    {
    }

    public sealed class RejectWithRequeueConsumeResult : ConsumeResult
    {
    }

    public sealed class RejectWithoutRequeueConsumeResult : ConsumeResult
    {
    }

    public class RetryConsumeResult : ConsumeResult
    {
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