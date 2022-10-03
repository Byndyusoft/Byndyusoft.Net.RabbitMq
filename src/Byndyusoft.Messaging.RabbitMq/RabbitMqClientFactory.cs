using System.Collections.Concurrent;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientFactory : IRabbitMqClientFactory
    {
        private readonly ConcurrentDictionary<string, RabbitMqClientHandler> _activeHandlers = new();
        private readonly IRabbitMqClientHandlerFactory _handlerFactory;

        public RabbitMqClientFactory(
            IRabbitMqClientHandlerFactory handlerFactory)
        {
            _handlerFactory = Preconditions.CheckNotNull(handlerFactory, nameof(handlerFactory));
        }

        public IRabbitMqClient CreateClient()
        {
            return CreateClient(Options.DefaultName);
        }

        public IRabbitMqClient CreateClient(string name)
        {
            Preconditions.CheckNotNull(name, nameof(name));

            var handler = _activeHandlers.GetOrAdd(name, _handlerFactory.CreateHandler);
            var client = new RabbitMqClient(handler);

            return client;
        }
    }
}