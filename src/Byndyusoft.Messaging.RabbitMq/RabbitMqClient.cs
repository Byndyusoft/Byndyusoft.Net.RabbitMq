using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClient : RabbitMqClientCore
    {
        public RabbitMqClient(IRabbitMqClientHandler handler, bool disposeHandler = false)
            : base(handler, disposeHandler)
        {
        }

        public RabbitMqClient(RabbitMqClientOptions options)
            : this(new RabbitMqClientHandler(options), true)
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