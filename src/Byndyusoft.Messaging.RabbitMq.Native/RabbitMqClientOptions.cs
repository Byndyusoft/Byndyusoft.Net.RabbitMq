using Byndyusoft.Messaging.RabbitMq.Native.ConnectionString;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Native
{
    public class RabbitMqClientOptions : RabbitMqClientCoreOptions
    {
        private readonly RabbitMqConnectionStringBuilder _connectionStringBuilder = new ();

        public string ConnectionString
        {
            get => _connectionStringBuilder.ConnectionString;
            set => _connectionStringBuilder.ConnectionString = Preconditions.CheckNotNull(value, nameof(ConnectionString));
        }

        internal RabbitMqConnectionStringBuilder ConnectionStringBuilder => _connectionStringBuilder;
    }
}