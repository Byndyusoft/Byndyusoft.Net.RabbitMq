namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public enum ClientConsumeResult
    {
        Ack,
        RejectWithRequeue,
        RejectWithoutRequeue,
        Error
    }
}