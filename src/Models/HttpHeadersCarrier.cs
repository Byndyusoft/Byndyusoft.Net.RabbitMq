using System.Collections;
using System.Collections.Generic;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Models
{
    public class HttpHeadersCarrier : ITextMap
    {
        private readonly IDictionary<string, object> _dictionary;

        public HttpHeadersCarrier(IDictionary<string, object> dictionary)
        {
            _dictionary = dictionary ?? new Dictionary<string, object>();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public void Set(string key, string value)
        {
            _dictionary.Add(key, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}