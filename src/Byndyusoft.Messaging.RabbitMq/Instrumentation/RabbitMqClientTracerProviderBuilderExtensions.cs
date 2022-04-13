// ReSharper disable CheckNamespace

using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Core.Diagnostics;

namespace OpenTelemetry.Trace
{
    /// <summary>
    ///     Extension method for setting up RabbitMqClient OpenTelemetry tracing.
    /// </summary>
    public static class RabbitMqClientTracerProviderBuilderExtensions
    {
        /// <summary>
        ///     Subscribes to the RabbitMqClient activity source to enable OpenTelemetry tracing.
        /// </summary>
        public static TracerProviderBuilder AddRabbitMqClientInstrumentation(this TracerProviderBuilder builder)
        {
            Preconditions.CheckNotNull(builder, nameof(builder));

            return builder.AddSource(RabbitMqClientActivitySource.Name);
        }
    }
}