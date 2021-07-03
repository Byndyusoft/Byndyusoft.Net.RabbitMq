using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Шаг пайплайна отправки исходящего сообщения
    /// </summary>
    /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
    public interface IProducePipe<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Обрабатывает входящее сообщение и передаёт его дальше
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        /// <param name="message">Соообщение</param>
        Task<IMessage<TMessage>> Pipe(IMessage<TMessage> message);
    }
}