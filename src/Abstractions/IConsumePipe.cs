using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Шаг пайплайна обработки входящего сообщения
    /// </summary>
    /// <typeparam name="TMessage">Тип входящего сообщения</typeparam>
    public interface IConsumePipe<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Обрабатывает входящее сообщение и передаёт его дальше
        /// </summary>
        /// <typeparam name="TMessage">Тип соообщения</typeparam>
        /// <param name="message">Соообщение</param>
        Task<TMessage> Pipe(TMessage message);
    }
}