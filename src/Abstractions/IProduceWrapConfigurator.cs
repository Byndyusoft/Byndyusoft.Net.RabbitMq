namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор обёрток пайплайна отправки исходящего сообщения
    /// </summary>
    public interface IProduceWrapConfigurator<TMessage> : IProduceReturnedPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет обёртку для отправки исходящего
        /// </summary>
        /// <typeparam name="TWrapper">Тип обёртки-обработчика</typeparam>
        IProduceWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IProduceMiddleware<TMessage>;
    }
}
