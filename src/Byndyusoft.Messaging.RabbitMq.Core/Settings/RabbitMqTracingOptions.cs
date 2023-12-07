namespace Byndyusoft.Messaging.RabbitMq.Settings
{
    public class RabbitMqTracingOptions
    {
        public bool LogEventsInTrace { get; set; } = false;

        public bool LogEventsInLog { get; set; } = true;
    }
}