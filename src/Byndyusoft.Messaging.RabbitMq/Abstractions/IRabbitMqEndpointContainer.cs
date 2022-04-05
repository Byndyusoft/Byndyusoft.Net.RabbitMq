namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public interface IRabbitMqEndpointContainer
    {
        RabbitMqEndpoint Endpoint { get; }
    }
}