namespace Byndyusoft.Messaging.RabbitMq
{
    public enum AckResult
    {
        Ack,
        NackWithRequeue,
        NackWithoutRequeue,
        Exception
    }
}