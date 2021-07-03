namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор пайплайна обработки входящего сообщения до пользовательского обработчика
    /// </summary>
    public interface IConsumeBeforePipeConfigurator<TMessage> where TMessage : class
    { 
        /// <summary>
        ///     Добавляет шаг в пайплайн обработки входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IConsumeBeforePipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IConsumePipe<TMessage>;
        
        /// <summary>
        ///     Отмечает место в пайплайне, где будет вызывана пользовательская обработка сообщения
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        IConsumeAfterPipeConfigurator<TMessage> Consume();
    }
}