using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    internal static class ActivityContextPropagation
    {
        public static readonly TextMapPropagator[] DefaultPropagators =
        {
            new TraceContextPropagator(),
            new BaggagePropagator()
        };

        public static void InjectContext(Activity? activity, RabbitMqMessageHeaders headers) =>
            InjectContext(activity, headers, DefaultPropagators);

        public static void ExtractContext(Activity? activity, RabbitMqMessageHeaders? headers) =>
            ExtractContext(activity, headers, DefaultPropagators);

        public static void InjectContext(
            Activity? activity, 
            RabbitMqMessageHeaders headers,
            IEnumerable<TextMapPropagator> propagators)
        {
            static void Setter(RabbitMqMessageHeaders h, string key, string value)
            {
                h[key] = value;
            }

            if (activity is null)
                return;

            var propagationContext =
                new PropagationContext(activity.Context, new Baggage().SetBaggage(activity.Baggage));
            foreach (var propagator in propagators) propagator.Inject(propagationContext, headers, Setter);
        }

        public static void ExtractContext(
            Activity? activity, 
            RabbitMqMessageHeaders? headers,
            IEnumerable<TextMapPropagator> propagators)
        {
            if (headers == null || activity is null)
                return;

            static string[] Getter(RabbitMqMessageHeaders h, string key)
            {
                if (h.TryGetValue(key, out var value))
                    return new[] {(string) value!};
                return Array.Empty<string>();
            }

            var propagationContext = new PropagationContext();
            foreach (var propagator in propagators)
                propagationContext = propagator.Extract(propagationContext, headers, Getter);

            var activityContext = propagationContext.ActivityContext;
            if (activityContext.IsValid())
            {
                activity.SetParentId(activityContext.TraceId, activityContext.SpanId, activityContext.TraceFlags);
                activity.TraceStateString = activityContext.TraceState;
            }
            
            foreach (var item in propagationContext.Baggage) activity.SetBaggage(item.Key, item.Value);
        }
    }
}