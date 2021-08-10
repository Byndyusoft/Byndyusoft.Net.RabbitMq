using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Шаг пайплайна обработки входящего сообщения
    /// </summary>
    public interface IConsumePipe
    {
        /// <summary>
        ///     Обрабатывает входящее сообщение и передаёт его дальше
        /// </summary>
        /// <param name="message">Соообщение</param>
        Task Pipe(object message);
    } 

    /// <summary>
    ///     Шаг пайплайна обработки входящего сообщения
    /// </summary>
    /// <typeparam name="TMessage">Тип входящего сообщения</typeparam>
    public interface IConsumePipe<TMessage> : IConsumePipe where TMessage : class
    {
        /// <summary>
        ///     Обрабатывает входящее сообщение и передаёт его дальше
        /// </summary>
        /// <typeparam name="TMessage">Тип соообщения</typeparam>
        /// <param name="message">Соообщение</param>
        Task<TMessage> Pipe(TMessage message);
    }
}