using System;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Propagation;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares
{ 
    /// <summary>
    ///     Middleware for tracing of producing messages
    /// </summary>
    /// <typeparam name="TMessage">Producing message type</typeparam>
    public sealed class TracerProduceMiddleware<TMessage> : IProduceMiddleware<TMessage> where TMessage : class
    {
        private readonly ITracer _tracer;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="tracer">Tracer</param>
        public TracerProduceMiddleware(ITracer tracer)
        {
            _tracer = tracer;
        }

        /// <summary>
        ///     Adds tracing around message producing
        /// </summary>
        /// <param name="message">Producing message</param>
        /// <param name="next">Next middleware in a chain</param>
        public async Task Handle(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next)
        {
            if (_tracer.ActiveSpan == null)
                throw new InvalidOperationException("No active tracing span. Push to queue will broken service chain");

            var span = _tracer.BuildSpan(nameof(Handle)).Start();

            span.SetTag(nameof(message), JsonConvert.SerializeObject(message));

            var carrier = new HttpHeadersCarrier(message.Properties.Headers);

            _tracer.Inject(_tracer.ActiveSpan.Context, BuiltinFormats.HttpHeaders, carrier);

            await next(message);

            span.Finish();
        }
    }
}