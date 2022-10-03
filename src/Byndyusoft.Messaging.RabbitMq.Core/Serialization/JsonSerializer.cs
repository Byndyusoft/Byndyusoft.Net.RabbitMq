namespace Byndyusoft.Messaging.RabbitMq.Serialization
{
    internal static class JsonSerializer
    {
        public static string? Serialize(object? value, RabbitMqDiagnosticsOptions options)
        {
            if (value is null)
                return null;

            using var stream = new StringLimitStream(options.ValueMaxStringLength);

            System.Text.Json.JsonSerializer.SerializeAsync(stream, value, options.JsonSerializerOptions)
                .GetAwaiter().GetResult();

            return stream.GetString();
        }
    }
}