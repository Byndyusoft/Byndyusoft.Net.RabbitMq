// ReSharper disable CheckNamespace

using System;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Settings;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.DependencyInjection;

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
        public static TracerProviderBuilder AddRabbitMqClientInstrumentation(
            this TracerProviderBuilder builder,
            Action<RabbitMqTracingOptions>? configureTracingOptions = null)
        {
            Preconditions.CheckNotNull(builder, nameof(builder));

            if (configureTracingOptions != null)
            {
                builder.ConfigureServices(services => services.Configure(configureTracingOptions));
            }

            return builder.AddSource(RabbitMqClientActivitySource.Name);
        }
    }
}