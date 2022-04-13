using System;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class RabbitMqMessageProperties
    {
        /// <summary>
        ///     Application Id.
        /// </summary>
        public string? AppId { get; set; }

        /// <summary>
        ///     MIME Content type
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        ///     MIME content encoding
        /// </summary>
        public string? ContentEncoding { get; set; }

        /// <summary>
        ///     Message priority, 0 to 9
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        ///     Helps correlate requests with responses
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        ///     Carries response queue name
        /// </summary>
        public string? ReplyTo { get; set; }

        /// <summary>
        ///     Application-specific message type, e.g. "orders.created"
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        ///     Arbitrary message ID
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        ///     Message expiration specification.
        /// </summary>
        public TimeSpan? Expiration { get; set; }

        /// <summary>
        ///     Message timestamp.
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        ///     User Id.
        /// </summary>
        public string? UserId { get; set; }
    }
}