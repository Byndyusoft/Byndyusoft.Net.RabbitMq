namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public enum HandlerConsumeResult
    {
        Ack,
        RejectWithRequeue,
        RejectWithoutRequeue
    }
}