using System;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Error during message producing\consuming
    /// </summary>
    public class ProcessMessageException : Exception
    {
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="message">Error description</param>
        public ProcessMessageException(string message)
            : base(message)
        {
        }
    }
}