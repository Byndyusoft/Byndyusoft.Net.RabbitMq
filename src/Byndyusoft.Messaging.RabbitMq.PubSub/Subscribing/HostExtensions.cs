namespace Byndyusoft.Messaging.RabbitMq.PubSub.Subscribing
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Linq;
    using System.Reflection;

    public static class HostExtensions
    {
        private static Type? GetMessageType(Type type, Type requiredBaseType)
        {
            for (var baseType = type; baseType != null; baseType = baseType.BaseType)
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == requiredBaseType)
                    return baseType.GetGenericArguments().Single();

            return null;
        }

        public static IHost StartKafkaProcessing(this IHost host)
        {
            var rabbitMqClient = host.Services.GetRequiredService<IRabbitMqClient>();

            var requiredBaseType = typeof(RabbitMqMessageHandlerBase<>);
            var messageHandlerDescriptors = host.Services.GetServices<IRabbitMqMessageHandler>()
                .Select(
                    x =>
                    {
                        var profileType = x.GetType();
                        return (
                            messageType: GetMessageType(profileType, requiredBaseType),
                            handlerAttribute: profileType.GetCustomAttribute<RabbitMqMessageHandlerAttribute>(false),
                            handler: x
                        );
                    }
                )
                .Where(x => x is {messageType: not null, handlerAttribute: not null});

            foreach (var messageHandlerDescriptor in messageHandlerDescriptors)
                rabbitMqClient.Subscribe(
                    messageHandlerDescriptor.handlerAttribute.QueueName,
                    (message, token) =>
                        messageHandlerDescriptor.handler.Handle(
                            message.Content,
                            token
                        )
                );

            return host;
        }
    }
}