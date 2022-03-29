using System;
using System.Reflection;
using OpenTracing;
using OpenTracing.Tag;
using Prometheus;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics
{
    /// <inheritdoc cref="IConsumeMetricsHandler" />
    public class ConsumeMetricsHandler : IConsumeMetricsHandler
    {
        /// <summary>
        ///     Prometheus histogram for consuming messages duration
        /// </summary>
        private readonly Histogram _handleTimeHistogram;

        /// <summary>
        ///     Tracer
        /// </summary>
        private readonly ITracer _tracer;

        /// <summary>
        ///     Ctor
        /// </summary>
        public ConsumeMetricsHandler(ITracer tracer)
        {
            _tracer = tracer;

            _handleTimeHistogram = Prometheus.Metrics
                .CreateHistogram($"{GetServiceSnakeName()}_processed_message_duration_seconds",
                    $"Message processed duration in seconds by {GetServiceName()}",
                    new HistogramConfiguration
                    {
                        Buckets = new double[] {1, 2, 3, 5, 7, 10, 14, 20},
                        LabelNames = new[] {"message_type"}
                    });
        }

        /// <inheritdoc />
        public void OnConsumed(string messageType, bool hasError, TimeSpan duration)
        {
            if (hasError) _tracer.ActiveSpan.SetTag(Tags.Error, true);

            SaveProcessingDuration(messageType, duration);
        }

        private void SaveProcessingDuration(string messageType, TimeSpan processingDuration)
        {
            _handleTimeHistogram.Labels(messageType)
                .Observe(processingDuration.TotalSeconds);
        }

        private string GetServiceName()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name;
        }

        private string GetServiceSnakeName()
        {
            return ToSnakeCase(GetServiceName());
        }

        private string ToSnakeCase(string assemblyName)
        {
            var parts = assemblyName.Split(".");

            return string.Join('_', parts).ToLowerInvariant();
        }
    }
}