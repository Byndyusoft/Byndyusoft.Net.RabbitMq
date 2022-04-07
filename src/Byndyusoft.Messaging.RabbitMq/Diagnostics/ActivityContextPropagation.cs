using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    internal static class ActivityContextPropagation
    {
        private static readonly TextMapPropagator[] Propagators =
        {
            new TraceContextPropagator(),
            new BaggagePropagator()
        };

        public static void InjectContext(Activity? activity, RabbitMqMessageHeaders headers)
        {
            static void Setter(RabbitMqMessageHeaders h, string key, string value)
            {
                h[key] = value;
            }

            if (activity is null)
                return;

            var propagationContext =
                new PropagationContext(activity.Context, new Baggage().SetBaggage(activity.Baggage));
            foreach (var propagator in Propagators) propagator.Inject(propagationContext, headers, Setter);
        }

        public static void ExtractContext(Activity? activity, RabbitMqMessageHeaders? headers)
        {
            if (headers == null || activity is null)
                return;

            static string[] Getter(RabbitMqMessageHeaders h, string key)
            {
                var value = h[key] as string;
                return new[] {value!};
            }

            var propagationContext = new PropagationContext();
            foreach (var propagator in Propagators)
                propagationContext = propagator.Extract(propagationContext, headers, Getter);

            var activityContext = propagationContext.ActivityContext;
            activity.SetParentId(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags);
            activity.TraceStateString = activityContext.TraceState;

            foreach (var item in propagationContext.Baggage) activity.SetBaggage(item.Key, item.Value);
        }
    }
}