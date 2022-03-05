using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqConsumedMessage : IDisposable
    {
        public IDictionary<string, object?>? Headers { get; set; }

        public string? ExchangeName { get; private set; }

        public string QueueName { get; private set; } = default!;

        /// <summary>
        ///     Gets a set of properties for the RabbitMq request.
        /// </summary>
        public RabbitMqMessageProperties Properties { get; private set; } = new();

        public ulong DeliveryTag { get; private set; }

        /// <summary>
        /// Retrieve the redelivered flag for this message.
        /// </summary>
        public bool Redelivered { get; private set; }

        /// <summary>
        /// Retrieve the routing key with which this message was published.
        /// </summary>
        public string RoutingKey { get; private set; } = default!;

        /// <summary>
        ///     Gets or sets the contents of the RabbitMq message.
        /// </summary>
        public HttpContent Content { get; private set; } = default!;

        public void Dispose()
        {
            Content.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}