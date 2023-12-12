using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Settings
{
    public class RabbitMqTracingOptions
    {
        private RabbitMqDiagnosticsOptions _diagnosticsOptions = new();

        public RabbitMqDiagnosticsOptions DiagnosticsOptions
        {
            get => _diagnosticsOptions;
            set => _diagnosticsOptions = Preconditions.CheckNotNull(value, nameof(RabbitMqDiagnosticsOptions));
        }

        public bool LogEventsInTrace { get; set; } = false;

        public bool LogEventsInLogs { get; set; } = true;
    }
}