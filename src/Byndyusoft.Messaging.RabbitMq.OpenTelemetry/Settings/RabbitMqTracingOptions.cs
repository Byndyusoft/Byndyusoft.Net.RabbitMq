using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Settings
{
    public class RabbitMqTracingOptions
    {
        public RabbitMqTracingOptions()
        {
            LogEventsInTrace = false;
            LogEventsInLogs = true;
            TagRequestParamsInTrace = true;
            EnrichLogsWithParams = true;
            EnrichLogsWithQueueInfo = true;
            RecordExceptions = true;
        }

        private RabbitMqDiagnosticsOptions _diagnosticsOptions = new();

        public RabbitMqDiagnosticsOptions DiagnosticsOptions
        {
            get => _diagnosticsOptions;
            set => _diagnosticsOptions = Preconditions.CheckNotNull(value, nameof(RabbitMqDiagnosticsOptions));
        }

        public bool LogEventsInTrace { get; set; }

        public bool LogEventsInLogs { get; set; }

        public bool TagRequestParamsInTrace { get; set; }

        public bool EnrichLogsWithParams { get; set; }

        public bool EnrichLogsWithQueueInfo { get; set; }

        public bool RecordExceptions { get; set; }
    }
}