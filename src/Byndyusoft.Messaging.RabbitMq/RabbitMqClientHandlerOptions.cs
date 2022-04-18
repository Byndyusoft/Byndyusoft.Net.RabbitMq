using Byndyusoft.Messaging.RabbitMq.Abstractions;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandlerOptions : RabbitMqClientOptions
    {
        public string ConnectionString { get; set; } = default!;
    }
}