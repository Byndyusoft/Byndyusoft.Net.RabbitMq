using System;
using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Шаг пайплайна обработки входящего сообщения
    /// </summary>
    /// <typeparam name="TMessage">Тип соообщения</typeparam>
    public interface IConsumeErrorPipe<TMessage> where TMessage : class
    {
        /// <summary>
        ///     Обрабатывает ошибку обработки входящего и передаёт её дальше
        /// </summary>
        /// <typeparam name="TMessage">Тип соообщения</typeparam>
        /// <param name="message">Соообщение</param>
        /// <param name="e">Ошибка при обработке сообщения</param>
        Task<(TMessage, Exception)> Pipe(TMessage message, Exception e);
    }
}