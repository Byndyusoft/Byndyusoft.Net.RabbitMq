using System;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqException : Exception
    {
        public InMemoryRabbitMqException(string message) : base(message)
        {
        }
    }
}