namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqEndpointContainer
    {
        RabbitMqEndpoint Endpoint { get; }
    }
}