namespace Byndyusoft.Messaging.RabbitMq
{
    public enum HandlerConsumeResult
    {
        Ack,
        RejectWithRequeue,
        RejectWithoutRequeue
    }
}