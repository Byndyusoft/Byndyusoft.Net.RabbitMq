using System.Diagnostics;
using Byndyusoft.Metrics.Extensions;
using Byndyusoft.Metrics.Listeners;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics
{
    /// <summary>
    ///     Listener for consuming messages metrics
    /// </summary>
    public class ConsumeListener : ActivityListenerBase
    {
        /// <summary>
        ///     
        /// </summary>
        private readonly IConsumeMetricsHandler _consumeMetricsHandler;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="consumeMetricsHandler"></param>
        public ConsumeListener(IConsumeMetricsHandler consumeMetricsHandler)
        {
            _consumeMetricsHandler = consumeMetricsHandler;
        }

        /// <inheritdoc />
        protected override void ActivityStarted(Activity activity)
        {
        }

        /// <inheritdoc />
        protected override void ActivityStopped(Activity activity)
        {
            var messageType = activity.GetMessageType();

            if (string.IsNullOrEmpty(messageType) == false)
                _consumeMetricsHandler.OnConsumed(messageType, activity.HasError(), activity.Duration);
        }

        /// <inheritdoc />
        protected override bool ShouldListenTo(ActivitySource activitySource)
        {
            if (activitySource.Name.StartsWith("MetricsConsumeMiddleware"))
                return true;

            return false;
        }
    }
}