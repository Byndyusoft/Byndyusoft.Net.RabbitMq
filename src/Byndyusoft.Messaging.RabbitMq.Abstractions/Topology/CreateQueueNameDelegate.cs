namespace Byndyusoft.Messaging.RabbitMq.Topology
{
    public delegate string CreateQueueNameDelegate(string exchange, string routingKey, string application);
}