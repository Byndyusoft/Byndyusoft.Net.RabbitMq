using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq.Native
{
    public class RabbitMqClientHandlerFactory : IRabbitMqClientHandlerFactory
    {
        private readonly ILoggerFactory? _loggerFactory;
        private readonly IOptionsMonitor<RabbitMqClientOptions> _options;

        public RabbitMqClientHandlerFactory(
            IOptionsMonitor<RabbitMqClientOptions> options,
            ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
            _options = Preconditions.CheckNotNull(options, nameof(options));
        }

        public RabbitMqClientHandler CreateHandler(string name)
        {
            var options = _options.Get(name);

            var logger = _loggerFactory?.CreateLogger<RabbitMqClientHandler>();
            return new RabbitMqClientHandler(Options.Create(options), logger);
        }
    }
}