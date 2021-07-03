namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор обёрток пайплайна отправки исходящего сообщения
    /// </summary>
    public interface IProduceWrapConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет обёртку для отправки исходящего
        /// </summary>
        /// <typeparam name="TWrapper">Тип обёртки-обработчика</typeparam>
        IProduceWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IProduceWrapper<TMessage>;
        
        /// <summary>
        ///     Добавляет шаг в пайплайн отправки исходящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IProduceBeforePipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IProducePipe<TMessage>;
    }
}
