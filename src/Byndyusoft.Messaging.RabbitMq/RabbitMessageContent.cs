using System.Net.Http;

namespace Byndyusoft.Messaging.RabbitMq
{
    internal static class RabbitMessageContent
    {
        public static HttpContent Create(HttpContent content)
        {
            var body = content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            var result = new ByteArrayContent(body);
            foreach (var header in content.Headers) result.Headers.Add(header.Key, header.Value);

            return result;
        }
    }
}