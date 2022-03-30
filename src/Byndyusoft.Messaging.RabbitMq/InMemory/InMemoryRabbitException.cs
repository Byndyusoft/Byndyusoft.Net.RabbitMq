using System;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitException : Exception
    {
        public InMemoryRabbitException(string message) : base(message)
        {
        }
    }
}