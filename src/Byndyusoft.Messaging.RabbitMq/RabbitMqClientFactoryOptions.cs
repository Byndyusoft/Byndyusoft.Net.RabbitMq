using System;

namespace Byndyusoft.Messaging.RabbitMq
{
    internal delegate IRabbitMqClientHandler CreateRabbitMqClientHandler(IServiceProvider serviceProvider);

    internal class RabbitMqClientFactoryOptions
    {
        public CreateRabbitMqClientHandler? CreateHandlerFunc { get; set; }
    }
}