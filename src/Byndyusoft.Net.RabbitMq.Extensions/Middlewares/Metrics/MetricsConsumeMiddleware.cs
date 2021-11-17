using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics
{
    /// <summary>
    ///     Middleware for gathering metrics on consume message
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class MetricsConsumeMiddleware<TMessage> : IConsumeMiddleware<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Source for measuring consuming activity
        /// </summary>
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(MetricsConsumeMiddleware<TMessage>));

        /// <summary>
        ///     Adds measure to activity source 
        /// </summary>
        /// <param name="message">Consuming message</param>
        /// <param name="next">Next middleware in a chain</param>
        public async Task Handle(IMessage<TMessage> message, Func<IMessage<TMessage>, Task> next)
        {
            using (var activity = ActivitySource.StartActivity(nameof(Handle)))
            {
                activity?.SetMessageType(typeof(TMessage).Name);

                try
                {
                    await next(message).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    activity?.AddBaggage("error", true.ToString());
                    throw;
                }
            }
        }
    }
}
