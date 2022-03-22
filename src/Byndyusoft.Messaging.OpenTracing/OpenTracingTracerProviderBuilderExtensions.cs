using System;
using OpenTelemetry.Trace;
using OpenTracing;

namespace Byndyusoft.Messaging.OpenTracing
{
    public static class OpenTracingTracerProviderBuilderExtensions
    {
        public static TracerProviderBuilder AddOpenTracingExporter(this TracerProviderBuilder builder, params string[] names)
        {
            if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
            {
                return deferredTracerProviderBuilder.Configure((sp, _) =>
                {
                    AddOpenTracingExporter(builder, sp, names);
                });
            }


            return builder.AddInstrumentation(() => new OpenTracingInstrumentation(null, names));
        }

        private static TracerProviderBuilder AddOpenTracingExporter(this TracerProviderBuilder builder,
            IServiceProvider serviceProvider, string[] names)
        {

            return builder.AddInstrumentation(() =>
            {
                var tracer = serviceProvider.GetService(typeof(ITracer)) as ITracer;
                return new OpenTracingInstrumentation(tracer, names);
            });

        }
    }
}