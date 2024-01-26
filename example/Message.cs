using Byndyusoft.MaskedSerialization.Annotations.Attributes;
using Byndyusoft.Telemetry.Abstraction.Attributes;

namespace Byndyusoft.Net.RabbitMq
{
    public class Message
    {
        [TelemetryItem] public string Property { get; set; } = default!;

        [Masked] public string Secret { get; set; } = default!;
    }
}