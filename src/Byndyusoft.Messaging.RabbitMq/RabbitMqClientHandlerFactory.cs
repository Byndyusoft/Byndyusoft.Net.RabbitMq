using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandlerFactory : IRabbitMqClientHandlerFactory
    {
        private readonly IBusFactory _busFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsMonitor<RabbitMqClientOptions> _options;

        public RabbitMqClientHandlerFactory(
            IBusFactory busFactory,
            IOptionsMonitor<RabbitMqClientOptions> options,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _busFactory = Preconditions.CheckNotNull(busFactory, nameof(busFactory));
            _options = Preconditions.CheckNotNull(options, nameof(options));
        }

        public RabbitMqClientHandler CreateHandler(string name)
        {
            var options = _options.Get(name);
            var logger = _loggerFactory.CreateLogger<RabbitMqClientHandler>();

            return new RabbitMqClientHandler(Options.Create(options), _busFactory, logger);
        }
    }
}