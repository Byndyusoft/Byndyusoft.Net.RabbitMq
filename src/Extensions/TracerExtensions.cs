using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTracing;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Extensions
{
    public static class TracerExtensions
    {
        public static ISpanContext? CreateSpanContextFromHeaders(this ITracer tracer,
            IDictionary<string, object> headers)
        {
            if (headers == null)
                return null;

            var dictionary = headers.ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[])x.Value));
            return tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(dictionary));
        }
    }
}