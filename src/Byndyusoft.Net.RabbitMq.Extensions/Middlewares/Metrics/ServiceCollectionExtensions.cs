using Byndyusoft.Metrics.Extensions;
using Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extensions for adding metrics infrastructure to DI
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds consuming messages metrics listener and handler to service collection
        /// </summary>
        public static IServiceCollection AddConsumeMetrics(this IServiceCollection services)
        {
            return services.AddMetricListener<ConsumeListener>()
                .AddSingleton<IConsumeMetricsHandler, ConsumeMetricsHandler>();
        }
    }
}