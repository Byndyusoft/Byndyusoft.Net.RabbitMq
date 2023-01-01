using System;
using System.Collections.Generic;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqEndpoint
    {
        public const ushort DefaultPort = 5672;

        public RabbitMqEndpoint() { }

        public RabbitMqEndpoint(string name, ushort port = DefaultPort)
            : this()
        {
            Port = port;
            Name = name;
        }

        public string Name { get; init; } = default!;

        public ushort Port { get; init; } = DefaultPort;

        public static RabbitMqEndpoint Parse(string input)
        {
            var parts = input.Split(":");

            var name = parts.Length > 0 ? parts[0] : "localhost";
            var port = parts.Length > 1 ? ushort.Parse(parts[1]) : DefaultPort;

            return new RabbitMqEndpoint(name, port);
        }

        public override string ToString()
        {
            var parts = new List<string> { Name };
            if (Port != DefaultPort)
            {
                parts.Add(Port.ToString());
            }

            return string.Join(":", parts);
        }

        protected bool Equals(RabbitMqEndpoint other) => Name == other.Name && Port == other.Port;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitMqEndpoint)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Name, Port);
    }
}