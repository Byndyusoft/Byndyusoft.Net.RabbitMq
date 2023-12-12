// ReSharper disable CheckNamespace

using System;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Settings;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            return builder
                .AddSource(RabbitMqClientActivitySource.Name)
                .AddInstrumentation(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<RabbitMqClientCoreOptions>>();
                    var listener = new RabbitMqActivityListener(options.Value);
                    return new RabbitMqActivityInstrumentation(listener);
                });
        }

        /// <summary>
        ///     Subscribes to the RabbitMqClient activity source to enable log tracing.
        /// </summary>
        public static TracerProviderBuilder AddRabbitMqClientLogInstrumentation(
            this TracerProviderBuilder builder)
        {
            Preconditions.CheckNotNull(builder, nameof(builder));

            return builder.AddInstrumentation(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMqLogListener>>();
                var options = sp.GetRequiredService<IOptions<RabbitMqClientCoreOptions>>();
                var listener = new RabbitMqLogListener(logger, options.Value);
                return new RabbitMqLogInstrumentation(listener);
            });
        }
    }
}