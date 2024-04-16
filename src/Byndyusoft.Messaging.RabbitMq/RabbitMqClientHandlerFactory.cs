using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandlerFactory : IRabbitMqClientHandlerFactory
    {
        private readonly IBusFactory _busFactory;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMqClientHandlerFactory(
            IBusFactory busFactory,
            ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? new NullLoggerFactory();
            _busFactory = Preconditions.CheckNotNull(busFactory, nameof(busFactory));
        }

        public RabbitMqClientHandler CreateHandler(string name, RabbitMqClientOptions options)
        {
            var logger = _loggerFactory.CreateLogger<RabbitMqClientHandler>();

            return new RabbitMqClientHandler(options, _busFactory, logger);
        }
    }
}