using Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Settings;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Serialization
{
    internal static class JsonSerializer
    {
        public static string? Serialize(object? value, RabbitMqDiagnosticsOptions options)
        {
            if (value is null)
                return null;

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            System.Text.Json.JsonSerializer.Serialize(stream, value, options.JsonSerializerOptions);

            return stream.GetString();
        }
    }
}