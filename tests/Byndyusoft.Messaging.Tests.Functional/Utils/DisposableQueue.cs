using System;
using System.Collections.Generic;
using EasyNetQ;
using EasyNetQ.Topology;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class DisposableQueue : IQueue, IDisposable
    {
        private readonly IQueue _inner;
        private readonly IAdvancedBus _rabbit;

        public DisposableQueue(IAdvancedBus rabbit, IQueue inner)
        {
            _inner = inner;
            _rabbit = rabbit;
        }

        public bool IsDurable => _inner.IsDurable;
        public bool IsAutoDelete => _inner.IsAutoDelete;
        public IDictionary<string, object> Arguments => _inner.Arguments;

        public void Dispose()
        {
            _rabbit.QueueDelete(_inner);
        }

        public string Name => _inner.Name;
        public bool IsExclusive => _inner.IsExclusive;
    }
}