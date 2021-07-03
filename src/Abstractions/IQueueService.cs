using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Служба работы с оцередями
    /// </summary>
    public interface IQueueService : IDisposable
    {
        /// <summary>
        ///     Инциализирует топологию очередй
        /// </summary>
        Task Initialize();
        
        /// <summary>
        ///     Публикует исходящее сообщение в шину
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        /// <param name="message">Исходящее сообщение</param>
        /// <param name="headers">Заголовки сообщения - необязательный параметр</param>
        /// <param name="returnedHandled">Обработчик вернувшихся сообщений - необязательный параметр</param>
        Task Publish<TMessage>(
            TMessage message,
            Dictionary<string, string>? headers = null,
            Action<MessageReturnedEventArgs>? returnedHandled = null) where TMessage : class;

        /// <summary>
        ///     Подписывается на вхожящие сообщения из шины
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        /// <param name="processMessage">Обработчик входящего сообщения</param>
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage);
        
        /// <summary>
        ///     Переотправляет входящие сообщения из всех очередей с ошибками
        /// </summary>
        Task ResendErrorMessages();
        
        /// <summary>
        ///     Переотправляет входящие сообщения из очереди с ошибками
        /// </summary>
        Task ResendErrorMessages(string routingKey);
    }
}
