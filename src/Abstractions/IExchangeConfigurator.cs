namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор обменника 
    /// </summary>
    public interface IExchangeConfigurator
    {
        /// <summary>
        ///     Добавляет пайплайн на обработку входящего сообщения
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения из очереди</typeparam>
        /// <param name="queueName">Имя очереди</param>
        /// <param name="routingKey">Ключ очереди</param>
        IConsumeWrapConfigurator<TMessage> Consume<TMessage>(string queueName, string routingKey) where TMessage : class;

        /// <summary>
        ///     Добавляет пайплайн на обработку входящего сообщения
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения из очереди</typeparam>
        IConsumeWrapConfigurator<TMessage> Consume<TMessage>() where TMessage : class;

        /// <summary>
        ///     Добавляет пайплайн на отправку исходящего сообщения
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения в очередь</typeparam>
        /// <param name="queueName">Имя очереди</param>
        /// <param name="routingKey">Ключ очереди</param>
        IProduceWrapConfigurator<TMessage> Produce<TMessage>(string queueName, string routingKey) where TMessage : class;

        /// <summary>
        ///     Добавляет пайплайн на отправку исходящего сообщения
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения в очередь</typeparam>
        IProduceWrapConfigurator<TMessage> Produce<TMessage>() where TMessage : class;
    }
}