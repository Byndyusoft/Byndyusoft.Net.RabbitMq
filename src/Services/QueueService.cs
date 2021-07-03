using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IQueueService" />
    public sealed class QueueService : IQueueService
    {
        /// <inheritdoc />
        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task Publish<TMessage>(TMessage message, Dictionary<string, string>? headers = null, Action<MessageReturnedEventArgs>? returnedHandled = null) where TMessage : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages(string routingKey)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}