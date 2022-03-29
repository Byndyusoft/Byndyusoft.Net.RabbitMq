using System.Text.Json;
using System.Text.Json.Serialization;

namespace Byndyusoft.Messaging.Serialization
{
    internal static class JsonConvert
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true
        };

        public static string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, Options);
        }
    }
}