using System;
using System.Diagnostics;
using System.Linq;
using OpenTracing;
using OpenTracing.Util;

namespace Byndyusoft.Messaging.OpenTracing
{
    public class OpenTracingInstrumentation : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly ITracer? _tracer;

        public OpenTracingInstrumentation(ITracer? tracer, string[] names)
        {
            _tracer = tracer;
            _listener = new ActivityListener
            {
                ShouldListenTo = source => names.Contains(source.Name),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = ActivityStarted,
                ActivityStopped = ActivityStopped
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        private void ActivityStarted(Activity activity)
        {
            var tracer = Tracer;
            if (tracer.ActiveSpan is null)
                return;

            var span = tracer.BuildSpan(activity.DisplayName).Start();
            activity.SetCustomProperty("opentracing-span", span);
        }

        private void ActivityStopped(Activity activity)
        {
            if (activity.GetCustomProperty("opentracing-span") is ISpan span)
            {
                foreach (var tag in activity.Tags)
                {
                    span.SetTag(tag.Key, tag.Value);
                }

                span.Finish();
            }
        }
        
        private ITracer Tracer => _tracer ?? GlobalTracer.Instance;
    }
}
