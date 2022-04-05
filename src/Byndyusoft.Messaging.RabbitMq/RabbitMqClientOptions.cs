using System.Diagnostics;
using System.Reflection;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientOptions
    {
        private QueueNamingConventions _namingConventions = new();
        private RabbitMqDiagnosticsOptions _diagnosticsOptions = new();

        public string ApplicationName { get; set; } =
            Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;

        public QueueNamingConventions NamingConventions
        {
            get => _namingConventions;
            set => _namingConventions = Preconditions.CheckNotNull(value, nameof(NamingConventions));
        }

        public RabbitMqDiagnosticsOptions DiagnosticsOptions
        {
            get => _diagnosticsOptions;
            set => _diagnosticsOptions = Preconditions.CheckNotNull(value, nameof(RabbitMqDiagnosticsOptions));
        }

        public string ConnectionString { get; set; } = default!;
    }
}