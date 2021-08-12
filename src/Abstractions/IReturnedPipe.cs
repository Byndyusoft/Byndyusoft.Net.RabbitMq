using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Шаг пайплайна обработки вернувшегося сообщения
    /// </summary>
    /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
    public interface IReturnedPipe<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Обрабатывает вернувшееся вхоядщее сообщение и передаёт его дальше
        /// </summary>
        /// <param name="args">Вернувшееся сообщение</param>
        Task<MessageReturnedEventArgs> Pipe(MessageReturnedEventArgs args);
    }
}