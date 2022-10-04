using System;
using System.Buffers;
using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Internal
{
    internal class FakeSerializer : ISerializer
    {
        const string MethodCallExceptionMessage = "ISerializer methods must not be called. Internal bug in Byndyusoft.Messaging.RabbitMq. Please, report it at https://github.com/Byndyusoft/Byndyusoft.Net.RabbitMq/issues";

        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            throw new InvalidOperationException(MethodCallExceptionMessage);
        }

        public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
        {
            throw new InvalidOperationException(MethodCallExceptionMessage);
        }
    }
}