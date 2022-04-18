namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public enum ConsumeResult
    {
        Ack,
        RejectWithRequeue,
        RejectWithoutRequeue,
        Error
    }
}