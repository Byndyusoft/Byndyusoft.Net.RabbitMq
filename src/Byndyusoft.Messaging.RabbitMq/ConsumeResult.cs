namespace Byndyusoft.Messaging.RabbitMq
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