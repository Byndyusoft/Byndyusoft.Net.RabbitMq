namespace Byndyusoft.Messaging.RabbitMq.Topology
{
    public enum QueueOverflowBehaviour
    {
        DropHead,
        RejectPublish,
        RejectPublishDlx
    }
}