namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна обработки входящего сообщения после пользовательского обработчика
    /// </summary>
    public interface IConsumeAfterPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет шаг в пайплайн обработки входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IConsumeAfterPipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IConsumePipe<TMessage>;

        /// <summary>
        ///     Добавляет шаг в пайплайн обработки ошибки при обработке входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TErrorPipe">Тип обработчика ошибки</typeparam>
        IConsumeErrorPipeConfigurator<TMessage> PipeError<TErrorPipe>() where TErrorPipe : IConsumeErrorPipe<TMessage>;
    }
}