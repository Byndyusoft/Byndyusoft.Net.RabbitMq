using System;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Error in pipeline configuration
    /// </summary>
    public class PipelineConfigurationException : Exception
    {
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="message">Error description</param>
        public PipelineConfigurationException(string message)
            : base(message)
        {
            
        }
    }
}