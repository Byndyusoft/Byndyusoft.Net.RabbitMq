using System;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор обёрток пайплайна обработки входящего сообщения 
    /// </summary>
    public interface IConsumeWrapConfigurator<TMessage> : IConsumeErrorPipeConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет обёртку для обработки входящего
        /// </summary>
        /// <typeparam name="TWrapper">Тип обёртки-обработчика</typeparam>
        IConsumeWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IConsumeWrapper<TMessage>;
    }
}
