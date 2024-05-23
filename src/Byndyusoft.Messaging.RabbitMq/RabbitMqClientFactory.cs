using System;
using System.Collections.Concurrent;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{ 
    internal class RabbitMqClientFactory : IRabbitMqClientFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, IRabbitMqClientHandler> _activeHandlers = new();
        private readonly IRabbitMqClientHandlerFactory _handlerFactory;
        private readonly IOptionsMonitor<RabbitMqClientOptions> _clientOptions;
        private readonly IOptionsMonitor<RabbitMqClientFactoryOptions> _factoryOptions;
        private readonly IServiceProvider _serviceProvider;

        public RabbitMqClientFactory(
            IRabbitMqClientHandlerFactory handlerFactory, 
            IOptionsMonitor<RabbitMqClientOptions> options, 
            IOptionsMonitor<RabbitMqClientFactoryOptions> factoryOptions,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _factoryOptions = Preconditions.CheckNotNull(factoryOptions, nameof(factoryOptions));
            _clientOptions = Preconditions.CheckNotNull(options, nameof(options));
            _handlerFactory = Preconditions.CheckNotNull(handlerFactory, nameof(handlerFactory));
        }

        public IRabbitMqClient CreateClient()
        {
            return CreateClient(Options.DefaultName);
        }

        public IRabbitMqClient CreateClient(string name)
        {
            Preconditions.CheckNotNull(name, nameof(name));

            var handler = _activeHandlers.GetOrAdd(name,
                _ =>
                {
                    var factoryOptions = _factoryOptions.Get(name);
                    if (factoryOptions?.CreateHandlerFunc is not null)
                        return factoryOptions.CreateHandlerFunc(_serviceProvider);

                    var clientOptions = _clientOptions.Get(name);
                    return _handlerFactory.CreateHandler(clientOptions);
                });
            return new RabbitMqClient(handler);
        }

        public void Dispose()
        {
            Disposable.MultiDispose(_activeHandlers.Values);
        }
    }
}