using System.Collections;
using System.Collections.Generic;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     RabbitMQ headers carrier for tracing
    /// </summary>
    public sealed class HttpHeadersCarrier : ITextMap
    {
        /// <summary>
        ///     Headers
        /// </summary>
        private readonly IDictionary<string, object> _dictionary;

        /// <summary>
        ///     Ctor
        /// </summary>
        public HttpHeadersCarrier(IDictionary<string, object> dictionary)
        {
            _dictionary = dictionary ?? new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public void Set(string key, string value)
        {
            _dictionary.Add(key, value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}