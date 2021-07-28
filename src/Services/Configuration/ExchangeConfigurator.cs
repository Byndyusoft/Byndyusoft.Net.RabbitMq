using System;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Services.Configuration
{
    /// <inheritdoc cref="IExchangeConfigurator"/>
    internal sealed class ExchangeConfigurator : IExchangeConfigurator
    {
        /// <summary>
        ///     Настройки обменника
        /// </summary>
        private readonly ExchangeConfiguration _exchangeConfiguration;
        
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="exchangeConfiguration">Настройки обменника</param>
        public ExchangeConfigurator(ExchangeConfiguration exchangeConfiguration)
        {
            _exchangeConfiguration = exchangeConfiguration ?? throw new ArgumentNullException(nameof(exchangeConfiguration));
        }

        public IConsumeWrapConfigurator<TMessage> Consume<TMessage>(string queueName, string routingKey)
            where TMessage : class
        {
            var queue = new QueueConfiguration(queueName, routingKey, typeof(TMessage));
            _exchangeConfiguration.ConsumeQueueConfigurations.Add(queue);
            return new ConsumeQueueConfigurator<TMessage>(queue);
        }
        public IConsumeWrapConfigurator<TMessage> Consume<TMessage>() where TMessage : class
        {
            var typeName = typeof(TMessage).FullName;
            return Consume<TMessage>(typeName, typeName);
        }

        public IProduceWrapConfigurator<TMessage> Produce<TMessage>(string queueName, string routingKey) where TMessage : class
        {
            var queue = new QueueConfiguration(queueName, routingKey, typeof(TMessage));
            _exchangeConfiguration.ProduceQueueConfigurations.Add(queue);
            return new ProduceQueueConfigurator<TMessage>(queue);
        }

        public IProduceWrapConfigurator<TMessage> Produce<TMessage>() where TMessage : class
        {
            var typeName = typeof(TMessage).FullName;
            return Produce<TMessage>(typeName, typeName);
        }
    }
}