namespace Byndyusoft.Messaging.RabbitMq.PubSub.Publishing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class RabbitMqMessagePublisher : IRabbitMqMessagePublisher
    {
        private class RabbitMqMessagePublishingProfileDescriptor
        {
            public string RoutingKey { get; }

            public string? Exchange { get; }

            public IRabbitMqMessagePublishingProfile PublishingProfile { get; }

            public RabbitMqMessagePublishingProfileDescriptor(
                string routingKey,
                string? exchange,
                IRabbitMqMessagePublishingProfile publishingProfile
            )
            {
                RoutingKey = routingKey;
                Exchange = exchange;
                PublishingProfile = publishingProfile;
            }
        }

        private readonly IRabbitMqClient _rabbitMqClient;
        private readonly IReadOnlyDictionary<Type, RabbitMqMessagePublishingProfileDescriptor> _publishingProfileDescriptorByMessageTypes;

        private static Type? GetMessageType(Type type, Type requiredBaseType)
        {
            for (var baseType = type; baseType != null; baseType = baseType.BaseType)
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == requiredBaseType)
                    return baseType.GetGenericArguments().Single();

            return null;
        }

        public RabbitMqMessagePublisher(
            IRabbitMqClient rabbitMqClient,
            IEnumerable<IRabbitMqMessagePublishingProfile> publishingProfiles
        )
        {
            _rabbitMqClient = rabbitMqClient ?? throw new ArgumentNullException(nameof(rabbitMqClient));

            var requiredBaseType = typeof(RabbitMqMessagePublishingProfileBase<>);
            _publishingProfileDescriptorByMessageTypes
                = publishingProfiles
                    .Select(
                        x =>
                        {
                            var profileType = x.GetType();
                            return (
                                messageType: GetMessageType(profileType, requiredBaseType),
                                profileAttribute: profileType.GetCustomAttribute<RabbitMqMessagePublishingProfileAttribute>(false),
                                profile: x
                            );
                        }
                    )
                    .Where(x => x is {messageType: not null, profileAttribute: not null})
                    .ToDictionary(
                        x => x.messageType!,
                        x => new RabbitMqMessagePublishingProfileDescriptor(
                            x.profileAttribute.RoutingKey,
                            x.profileAttribute.Exchange,
                            x.profile
                        )
                    );
        }

        public async Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_publishingProfileDescriptorByMessageTypes.TryGetValue(typeof(TMessage), out var publishingProfileDescriptor) == false)
                throw new InvalidOperationException($"Publishing profile for type {typeof(TMessage)} was not provided");

            await _rabbitMqClient.PublishMessageAsync(
                    new RabbitMqMessage
                    {
                        Exchange = publishingProfileDescriptor.Exchange,
                        RoutingKey = publishingProfileDescriptor.RoutingKey,
                        Content = publishingProfileDescriptor.PublishingProfile.GetMessageContent(message)
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }
}