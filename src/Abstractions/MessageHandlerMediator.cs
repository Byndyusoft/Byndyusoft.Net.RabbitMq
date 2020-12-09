using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    public class MessageHandlerMediator
    {
        private readonly Dictionary<string, IMessageHandler> _dictionary;

        public MessageHandlerMediator()
        {
            _dictionary = new Dictionary<string, IMessageHandler>();
        }

        public void Add(string routingKey, IMessageHandler handler)
        {
            _dictionary.Add(routingKey, handler);
        }

        public Task HandleAsync(string routingKey, string message)
        {
            var handler = _dictionary[routingKey];

            if (handler == null) 
                throw new NotSupportedException($"MessageHandlerMediator doesn't have a {routingKey}");

            return handler.Handle(message);
        }
    }
}