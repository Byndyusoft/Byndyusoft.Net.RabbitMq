using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Byndyusoft.Messaging.RabbitMq.Messages
{
    public static class RabbitMqMessageContent
    {
        public static HttpContent Create(ReadOnlyMemory<byte> body, RabbitMqMessageProperties properties)
        {
            var result = new ByteArrayContent(body.ToArray());

            if (properties.ContentType is not null)
                result.Headers.ContentType = new MediaTypeHeaderValue(properties.ContentType);

            if (properties.ContentEncoding is not null)
                result.Headers.ContentEncoding.Add(properties.ContentEncoding);

            return result;
        }

        public static HttpContent Create(HttpContent content)
        {
            var body = content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            var result = new ByteArrayContent(body);
            foreach (var header in content.Headers)
                result.Headers.Add(header.Key, header.Value);

            return result;
        }
    }
}