using System.Text.Json;
using System.Text.Json.Serialization;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqDiagnosticsOptions
    {
        public const int DefaultValueMaxStringLength = 2000;

        private JsonSerializerOptions _jsonSerializerOptions;
        private int? _valueMaxStringLength;

        public RabbitMqDiagnosticsOptions()
        {
            _jsonSerializerOptions = new JsonSerializerOptions
                {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            _valueMaxStringLength = DefaultValueMaxStringLength;
        }

        public JsonSerializerOptions JsonSerializerOptions
        {
            get => _jsonSerializerOptions;
            set => _jsonSerializerOptions = Preconditions.CheckNotNull(value, nameof(JsonSerializerOptions));
        }

        public int? ValueMaxStringLength
        {
            get => _valueMaxStringLength;
            set => _valueMaxStringLength = Preconditions.CheckNotNegative(value, nameof(ValueMaxStringLength));
        }
    }
}