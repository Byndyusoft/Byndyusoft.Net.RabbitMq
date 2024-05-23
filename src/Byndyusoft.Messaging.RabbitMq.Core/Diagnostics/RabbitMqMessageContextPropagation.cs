using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public static class RabbitMqMessageContextPropagation
    {
        private static readonly TraceContextPropagator TraceContextPropagator = new ();

        private static IEnumerable<TextMapPropagator> Propagators
        {
            get
            {
                yield return TraceContextPropagator;
                yield return OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator;
            }
        }

        internal static void InjectContext(
            Activity? activity, 
            RabbitMqMessageHeaders headers)
        {
            if (activity is null)
                return;

            var propagationContext =
                new PropagationContext(activity.Context, new Baggage().SetBaggage(activity.Baggage));
            foreach (var propagator in Propagators) 
                propagator.Inject(propagationContext, headers, Setter);
            return;

            static void Setter(RabbitMqMessageHeaders h, string key, string value)
            {
                h[key] = value;
            }
        }

        internal static PropagationContext ExtractContext(
            RabbitMqMessageHeaders headers)
        {
            var propagationContext = new PropagationContext();
            foreach (var propagator in Propagators)
                propagationContext = propagator.Extract(propagationContext, headers, Getter);
            
            return propagationContext;

            static string[] Getter(RabbitMqMessageHeaders h, string key)
            {
                if (h.TryGetValue(key, out var value))
                    return new[] {(string) value!};
                return Array.Empty<string>();
            }
        }
    }
}