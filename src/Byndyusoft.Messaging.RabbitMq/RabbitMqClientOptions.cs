using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientOptions : RabbitMqClientCoreOptions
    {
        private string _connectionString = default!;

        public string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = Preconditions.CheckNotNull(value, nameof(ConnectionString));
        }
    }
}