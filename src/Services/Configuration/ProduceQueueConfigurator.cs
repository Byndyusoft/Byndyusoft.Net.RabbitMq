using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc/>
    internal sealed class ProduceQueueConfigurator<TMessage> : IProduceWrapConfigurator<TMessage> where TMessage : class
    {
        private readonly QueueConfiguration _queue;

        public ProduceQueueConfigurator(QueueConfiguration queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public IProduceWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IProduceWrapper<TMessage>
        {
            _queue.Wrapers.Add(typeof(TWrapper));
            return this;
        }

        public IProduceReturnedPipeConfigurator<TMessage> PipeReturned<TReturnedPipe>() where TReturnedPipe : IReturnedPipe<TMessage>
        {
            _queue.Pipes.Add(typeof(TReturnedPipe));
            return this;
        }
    }
}