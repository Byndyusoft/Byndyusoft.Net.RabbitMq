using System;
using System.Diagnostics;
using System.Reflection;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientOptions
    {
        private RabbitMqDiagnosticsOptions _diagnosticsOptions = new();
        private QueueNamingConventions _namingConventions = new();
        private string _connectionString = default!;

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

        public TimeSpan? RpcLivenessCheckPeriod { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan? RpcIdleLifetime { get; set; } = null;

        public string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = Preconditions.CheckNotNull(value, nameof(ConnectionString));
        }
    }
}