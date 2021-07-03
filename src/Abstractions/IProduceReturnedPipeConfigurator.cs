namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна обработки вернувшегося исходящего
    /// </summary>
    /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
    public interface IProduceReturnedPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет шаг в пайплайн обработки ошибки при обработке входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        /// <typeparam name="TReturnedPipe">Тип обработчика вернувшегося исходящего</typeparam>
        IProduceReturnedPipeConfigurator<TMessage> PipeError<TReturnedPipe>() where TReturnedPipe : IReturnedPipe<TMessage>;
    }
}