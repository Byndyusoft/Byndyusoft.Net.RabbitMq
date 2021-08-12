using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc cref="IConsumeMiddlewareConfigurator&lt;TMessage&gt;"/>
    internal sealed class ConsumeQueueConfigurator<TMessage> : IConsumeMiddlewareConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Configuration of queue
        /// </summary>
        private readonly QueueConfiguration _queue;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="queue">Configuration of queue</param>
        public ConsumeQueueConfigurator(QueueConfiguration queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        /// <inheritdoc />
        public IConsumeMiddlewareConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IConsumeMiddleware<TMessage>
        {
            _queue.Middlewares.Add(typeof(TWrapper));
            return this;
        }

        /// <inheritdoc />
        public IConsumeErrorPipeConfigurator<TMessage> PipeError<TErrorPipe>() where TErrorPipe : IConsumeErrorPipe<TMessage>
        {
            _queue.Pipes.Add(typeof(TErrorPipe));
            return this;
        }
    }
}