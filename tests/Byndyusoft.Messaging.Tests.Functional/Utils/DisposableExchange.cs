using System;
using System.Collections.Generic;
using EasyNetQ;
using EasyNetQ.Topology;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class DisposableExchange : IExchange, IDisposable
    {
        private readonly IExchange _inner;
        private readonly IAdvancedBus _rabbit;

        public DisposableExchange(IAdvancedBus rabbit, IExchange inner)
        {
            _inner = inner;
            _rabbit = rabbit;
        }

        public string Type => _inner.Type;
        public bool IsDurable => _inner.IsDurable;
        public bool IsAutoDelete => _inner.IsAutoDelete;
        public IDictionary<string, object> Arguments => _inner.Arguments;

        public void Dispose()
        {
            _rabbit.ExchangeDelete(_inner);
        }

        public string Name => _inner.Name;
    }
}