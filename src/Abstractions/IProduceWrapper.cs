using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Обёртка для пайплайна отправки исходящего
    /// </summary>
    /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
    public interface IProduceWrapper<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Оборачивает пайплайн отправки исходящего
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего соообщения</typeparam>
        /// <param name="message">Соообщение</param>
        /// <param name="pipe">Пайплайн отправки</param>
        Task WrapPipe(IMessage<TMessage> message, IProducePipe<TMessage> pipe);
    }
}