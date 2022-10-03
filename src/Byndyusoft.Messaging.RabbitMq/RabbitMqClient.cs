using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Utils;
using OptionsCore = Microsoft.Extensions.Options.Options;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClient : RabbitMqClientCore
    {
        public RabbitMqClient(RabbitMqClientHandler handler, bool disposeHandler = false)
            : base(Preconditions.CheckNotNull(handler, nameof(handler)), handler.Options, disposeHandler)
        {
        }

        public RabbitMqClient(RabbitMqClientOptions options)
            : this(new RabbitMqClientHandler(OptionsCore.Create(options), new BusFactory()))
        {
        }

        public RabbitMqClient(string connectionString)
            : this(new RabbitMqClientOptions
            {
                ConnectionString = Preconditions.CheckNotNull(connectionString, nameof(connectionString))
            })
        {
        }
    }
}