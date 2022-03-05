using System;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Messaging.RabbitMq.DependencyInjection
{
    public static class RabbitMqClientServiceCollectionExtensions
    {
        public static IRabbitMqClientBuilder AddRabbitMq(this IServiceCollection services)
        {
            return null!;
        }
    }

    public static class RabbitMqClientExtensions
    {
        public static IRabbitMqMessageConsumerBuilder Consume(this IRabbitMqClient client, string queueName,
            Func<RabbitMqConsumedMessage, AckResult> handler)
        {
            return null!;
        }

        public static IRabbitMqMessageConsumerBuilder Consume<TContent>(this IRabbitMqClient client, string queueName,
            Func<TContent, RabbitMqConsumedMessage, AckResult> handler)
        {
            return null!;
        }
    }

    public interface IRabbitMqClientBuilder
    {

    }

    public interface IRabbitMqMessageConsumerBuilder
    {

    }
}