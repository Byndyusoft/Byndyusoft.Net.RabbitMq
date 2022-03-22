// ReSharper disable CheckNamespace

using System;
using Byndyusoft.Messaging.Core;
using Byndyusoft.Messaging.Instrumentation;

namespace OpenTelemetry.Trace
{
    /// <summary>
    ///     Extension method for setting up QueueService OpenTelemetry tracing.
    /// </summary>
    public static class QueueServiceTracerProviderBuilderExtensions
    {
        /// <summary>
        ///     Subscribes to the QueueService activity source to enable OpenTelemetry tracing.
        /// </summary>
        public static TracerProviderBuilder AddQueueServiceInstrumentation(
            this TracerProviderBuilder builder,
            Action<QueueServiceInstrumentationOptions>? _ = null)
        {
            return builder.AddSource(QueueServiceActivitySource.Name);
        }
    }
}