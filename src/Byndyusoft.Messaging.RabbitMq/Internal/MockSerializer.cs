using System;
using System.Buffers;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal class MockSerializer : ISerializer
    {
        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            throw new NotImplementedException();
        }

        public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
        {
            throw new NotImplementedException();
        }
    }
}