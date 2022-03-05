using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqMessage : IDisposable
    {
        public IDictionary<string, object?>? Headers { get; set; }

        public string? ExchangeName { get; set; }

        public string? RoutingKey { get; set; }

        public bool Mandatory { get; set; }

        public RabbitMqMessageProperties Properties { get; set; } = new();

        public HttpContent Content { get; set; } = default!;

        public void Dispose()
        {
            Content.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
