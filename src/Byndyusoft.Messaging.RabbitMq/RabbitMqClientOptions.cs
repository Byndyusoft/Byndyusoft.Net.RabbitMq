namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientOptions : RabbitMqClientCoreOptions
    {
        public string ConnectionString { get; set; } = default!;
    }
}