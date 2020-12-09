using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Byndyusoft.AspNetCore.RabbitMq.Models
{
    public class Exchange
    {
        public Exchange(
            string name,
            string type = ExchangeType.Direct,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            Name = name ??
                   throw new ArgumentNullException(name, $"{name} must not be null");

            Type = type;
            IsDurable = durable;
            IsAutoDelete = autoDelete;
            Arguments = arguments ?? new Dictionary<string, object>();
        }

        public string Name { get; }
        public string Type { get; }
        public bool IsDurable { get; }
        public bool IsAutoDelete { get; }
        public IDictionary<string, object> Arguments { get; }

        public static Exchange GetDefault()
        {
            return new Exchange("");
        }
    }
}