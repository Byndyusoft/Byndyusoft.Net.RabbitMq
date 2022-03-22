using System;

namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueMessageProperties //: IEnumerable<KeyValuePair<string, object?>>
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

        //public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        //{
        //    yield return new KeyValuePair<string, object?>(nameof(ContentType), ContentType);
        //    yield return new KeyValuePair<string, object?>(nameof(ContentEncoding), ContentEncoding);
        //    yield return new KeyValuePair<string, object?>(nameof(Priority), Priority);
        //    yield return new KeyValuePair<string, object?>(nameof(CorrelationId), CorrelationId);
        //    yield return new KeyValuePair<string, object?>(nameof(ReplyTo), ReplyTo);
        //    yield return new KeyValuePair<string, object?>(nameof(Type), Type);
        //    yield return new KeyValuePair<string, object?>(nameof(MessageId), MessageId);
        //    yield return new KeyValuePair<string, object?>(nameof(Expiration), Expiration);
        //    yield return new KeyValuePair<string, object?>(nameof(Timestamp), Timestamp);
        //    yield return new KeyValuePair<string, object?>(nameof(UserId), UserId);
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}
    }
}