using System;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор обёрток пайплайна обработки входящего сообщения 
    /// </summary>
    public interface IConsumeWrapConfigurator<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Добавляет обёртку для обработки входящего
        /// </summary>
        /// <typeparam name="TWrapper">Тип обёртки-обработчика</typeparam>
        IConsumeWrapConfigurator<TMessage> Wrap<TWrapper>() where TWrapper : IConsumeWrapper<TMessage>;
        
        /// <summary>
        ///     Добавляет шаг в пайплайн обработки входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип сообщения</typeparam>
        /// <typeparam name="TPipe">Тип обработчика</typeparam>
        IConsumeBeforePipeConfigurator<TMessage> Pipe<TPipe>() where TPipe : IConsumePipe<TMessage>;
    }
}
