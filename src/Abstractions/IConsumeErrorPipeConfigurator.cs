namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна обработки ошибки при обработке входящего
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IConsumeErrorPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет шаг в пайплайн обработки ошибки при обработке входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TErrorPipe">Тип обработчика ошибки</typeparam>
        IConsumeErrorPipeConfigurator<TMessage> PipeError<TErrorPipe>() where TErrorPipe : IConsumeErrorPipe<TMessage>;
    }
}