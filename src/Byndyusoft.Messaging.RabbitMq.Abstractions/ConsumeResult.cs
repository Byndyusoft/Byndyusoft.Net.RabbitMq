using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public abstract class ConsumeResult
    {
        public static readonly ConsumeResult Ack = new AckConsumeResult();

        public static readonly ConsumeResult RejectWithRequeue = new RejectWithRequeueConsumeResult();

        public static readonly ConsumeResult RejectWithoutRequeue = new RejectWithoutRequeueConsumeResult();

        public static readonly ConsumeResult Retry = new RetryConsumeResult();

        public static ConsumeResult Error(Exception? e = null)
        {
            return new ErrorConsumeResult(e);
        }

        public abstract string GetDescription();
    }

    public sealed class AckConsumeResult : ConsumeResult
    {
        public override string GetDescription()
        {
            return "Ack";
        }
    }

    public sealed class RejectWithRequeueConsumeResult : ConsumeResult
    {
        public override string GetDescription()
        {
            return "RejectWithRequeue";
        }
    }

    public sealed class RejectWithoutRequeueConsumeResult : ConsumeResult
    {
        public override string GetDescription()
        {
            return "RejectWithoutRequeue";
        }
    }

    public class RetryConsumeResult : ConsumeResult
    {
        public override string GetDescription()
        {
            return "Retry";
        }
    }

    public sealed class ErrorConsumeResult : ConsumeResult
    {
        public ErrorConsumeResult(Exception? exception)
        {
            Exception = exception;
        }

        public Exception? Exception { get; }

        public override string GetDescription()
        {
            var exceptionPart =
                Exception is null
                    ? "no exception"
                    : $"{Exception.GetType().Name} : {Exception.Message}";
            return $"Error ({exceptionPart})";
        }
    }
}