namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Api for builing pipeline for hadnling error on consuming message
    /// </summary>
    /// <typeparam name="TMessage">Type on incoming message</typeparam>
    public interface IConsumeErrorPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Adds pipeline 
        ///     Добавляет шаг в пайплайн обработки ошибки при обработке входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TErrorPipe">Тип обработчика ошибки</typeparam>
        IConsumeErrorPipeConfigurator<TMessage> PipeError<TErrorPipe>() where TErrorPipe : IConsumeErrorPipe<TMessage>;
    }
}