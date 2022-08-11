using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandlerFactory : IRabbitMqClientHandlerFactory
    {
        private readonly IBusFactory _busFactory;
        private readonly IOptionsSnapshot<RabbitMqClientOptions> _options;

        public RabbitMqClientHandlerFactory(
            IBusFactory busFactory,
            IOptionsSnapshot<RabbitMqClientOptions> options)
        {
            _busFactory = Preconditions.CheckNotNull(busFactory, nameof(busFactory));
            _options = Preconditions.CheckNotNull(options, nameof(options));
        }

        public RabbitMqClientHandler CreateHandler(string name)
        {
            var options = _options.Get(name);

            return new RabbitMqClientHandler(Options.Create(options), _busFactory);
        }
    }
}