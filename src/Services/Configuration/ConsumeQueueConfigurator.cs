using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc/>
    internal sealed class ConsumeQueueConfigurator<TMessage> : IConsumeWrapConfigurator<TMessage> where TMessage : class
    {
        private readonly QueueConfiguration _queue;

        public ConsumeQueueConfigurator(QueueConfiguration queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public IConsumeWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IConsumeWrapper<TMessage>
        {
            _queue.Wrapers.Add(typeof(TWrapper));
            return this;
        }

        public IConsumeErrorPipeConfigurator<TMessage> PipeError<TErrorPipe>() where TErrorPipe : IConsumeErrorPipe<TMessage>
        {
            _queue.Pipes.Add(typeof(TErrorPipe));
            return this;
        }
    }
}