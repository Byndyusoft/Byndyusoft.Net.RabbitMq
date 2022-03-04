using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc cref="IExchangeConfigurator" />
    internal sealed class ExchangeConfigurator : IExchangeConfigurator
    {
        /// <summary>
        ///     Configuration of exchange
        /// </summary>
        private readonly ExchangeConfiguration _exchangeConfiguration;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="exchangeConfiguration">Configuration of exchange</param>
        public ExchangeConfigurator(ExchangeConfiguration exchangeConfiguration)
        {
            _exchangeConfiguration =
                exchangeConfiguration ?? throw new ArgumentNullException(nameof(exchangeConfiguration));
        }

        /// <inheritdoc />
        public IConsumeMiddlewareConfigurator<TMessage> Consume<TMessage>(string queueName, string routingKey)
            where TMessage : class
        {
            var queue = new QueueConfiguration(queueName, routingKey, typeof(TMessage));
            _exchangeConfiguration.ConsumeQueueConfigurations.Add(queue);
            return new ConsumeQueueConfigurator<TMessage>(queue);
        }

        /// <inheritdoc />
        public IConsumeMiddlewareConfigurator<TMessage> Consume<TMessage>() where TMessage : class
        {
            var typeName = typeof(TMessage).FullName!;
            return Consume<TMessage>(typeName, typeName);
        }

        /// <inheritdoc />
        public IProduceMiddlewareConfigurator<TMessage> Produce<TMessage>(string queueName, string routingKey)
            where TMessage : class
        {
            var queue = new QueueConfiguration(queueName, routingKey, typeof(TMessage));
            _exchangeConfiguration.ProduceQueueConfigurations.Add(queue);
            return new ProduceQueueConfigurator<TMessage>(queue);
        }

        /// <inheritdoc />
        public IProduceMiddlewareConfigurator<TMessage> Produce<TMessage>() where TMessage : class
        {
            var typeName = typeof(TMessage).FullName!;
            return Produce<TMessage>(typeName, typeName);
        }
    }
}