using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    public delegate IRabbitMqClientHandler CreateRabbitMqClientHandler(IServiceProvider serviceProvider);

    public class RabbitMqClientFactoryOptions
    {
        public CreateRabbitMqClientHandler? CreateHandlerFunc { get; set; }
    }
}