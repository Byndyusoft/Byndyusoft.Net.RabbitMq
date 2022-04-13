namespace Byndyusoft.Messaging.RabbitMq.Abstractions.Topology
{
    public delegate string CreateQueueNameDelegate(string exchange, string routingKey, string application);
}