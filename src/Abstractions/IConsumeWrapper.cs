using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Обёртка для обработки входящего
    /// </summary>
    public interface IConsumeWrapper
    {
        /// <summary>
        ///     Оборачивает пайплайн обработки входящего
        /// </summary>
        /// <param name="message">Соообщение</param>
        /// <param name="pipe">Пайплайн обработки</param>
        Task WrapPipe(IMessage message, IConsumeWrapper pipe);
    }

    /// <summary>
    ///     Обёртка для обработки входящего
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IConsumeWrapper<TMessage>: IConsumeWrapper where TMessage : class
    {
        /// <summary>
        ///     Оборачивает пайплайн обработки входящего
        /// </summary>
        /// <typeparam name="TMessage">Тип соообщения</typeparam>
        /// <param name="message">Соообщение</param>
        /// <param name="pipe">Пайплайн обработки</param>
        Task WrapPipe(IMessage<TMessage> message, IConsumePipe<TMessage> pipe);
    }
}