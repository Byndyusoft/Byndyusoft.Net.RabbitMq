using System.Diagnostics;
using System.Reflection;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class RabbitMqClientOptions
    {
        private RabbitMqDiagnosticsOptions _diagnosticsOptions = new();
        private QueueNamingConventions _namingConventions = new();

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
    }
}