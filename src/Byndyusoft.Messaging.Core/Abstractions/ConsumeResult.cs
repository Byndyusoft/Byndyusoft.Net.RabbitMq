namespace Byndyusoft.Messaging.Abstractions
{
    public enum ConsumeResult
    {
        Ack,
        RejectWithRequeue,
        RejectWithoutRequeue,
        Error,
        Retry
    }
}