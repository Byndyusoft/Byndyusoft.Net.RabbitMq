using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public abstract class ConsumeResult
    {
        public static readonly ConsumeResult Ack = new AckConsumeResult();

        public static readonly ConsumeResult RejectWithRequeue = new RejectWithRequeueConsumeResult();

        public static readonly ConsumeResult RejectWithoutRequeue = new RejectWithoutRequeueConsumeResult();

        public static ConsumeResult Retry(string? reason = null) => new RetryConsumeResult(reason);

        public static ConsumeResult Error(Exception? e = null) => new ErrorConsumeResult(e);

        public static ConsumeResult Error(string message) => new ErrorConsumeResult(new Exception(message));

        public abstract string GetDescription();
    }

    public sealed class AckConsumeResult : ConsumeResult
    {
        public override string GetDescription() => "Ack";
    }

    public sealed class RejectWithRequeueConsumeResult : ConsumeResult
    {
        public override string GetDescription() => "RejectWithRequeue";
    }

    public sealed class RejectWithoutRequeueConsumeResult : ConsumeResult
    {
        public override string GetDescription() => "RejectWithoutRequeue";
    }

    public sealed class RetryConsumeResult : ConsumeResult
    {
        public RetryConsumeResult(string? reason)
        {
            Reason = reason;
        }

        public string? Reason { get; }

        public override string GetDescription() => Reason is null ? "Retry" : $"Retry: {Reason}";
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
                    ? ""
                    : $" ({Exception.GetType().Name} : {Exception.Message})";
            return $"Error{exceptionPart}";
        }
    }
}