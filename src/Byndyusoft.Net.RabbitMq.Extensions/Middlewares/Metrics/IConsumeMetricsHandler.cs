using System;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics
{
    /// <summary>
    ///     Handler for pushing metrics on consuming messages from queue
    /// </summary>
    public interface IConsumeMetricsHandler
    {
        /// <summary>
        ///     Pushes metrics when message has been consumed
        /// </summary>
        /// <param name="messageType">Name of message type</param>
        /// <param name="hasError">True, if message was consumed with failure</param>
        /// <param name="duration">Duration of message consuming</param>
        void OnConsumed(string messageType, bool hasError, TimeSpan duration);
    }
}