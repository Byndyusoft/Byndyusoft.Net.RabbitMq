namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна отправки исходящего сообщения до публикации в шину
    /// </summary>
    public interface IProduceBeforePipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет шаг в пайплайн отправки исходящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IProduceBeforePipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IConsumePipe<TMessage>;
        
        /// <summary>
        ///     Отмечает место в пайплайне, где сообщение будет опубликовано в шину
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        IProduceAfterPipeConfigurator<TMessage> Produce();
    }
}