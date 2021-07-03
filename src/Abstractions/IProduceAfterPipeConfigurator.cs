namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна отправки исходящего сообщения после публикации в шину
    /// </summary>
    public interface IProduceAfterPipeConfigurator<TMessage> where TMessage : class
    { 
        /// <summary>
        ///     Добавляет шаг в пайплайн отправки исходящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IProduceAfterPipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IProducePipe<TMessage>;

        /// <summary>
        ///     Добавляет шаг в пайплайн обработки вернувшегося исходящего
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IProduceReturnedPipeConfigurator<TMessage> PipeReturned<TPipe>() where TPipe : IReturnedPipe<TMessage>;
    }
}