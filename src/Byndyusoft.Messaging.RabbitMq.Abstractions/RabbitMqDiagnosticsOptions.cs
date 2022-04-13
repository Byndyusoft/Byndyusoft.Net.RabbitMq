using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class RabbitMqDiagnosticsOptions
    {
        public const int DefaultValueMaxStringLength = 2000;

        private JsonSerializerOptions _jsonSerializerOptions;
        private int? _valueMaxStringLength;

        public RabbitMqDiagnosticsOptions()
        {
            _jsonSerializerOptions = new JsonSerializerOptions()
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