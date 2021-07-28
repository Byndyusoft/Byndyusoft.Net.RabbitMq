using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Фабрика подключений к шине RabbitMq
    /// </summary>
    public interface IBusFactory
    {
        /// <summary>
        ///     Возвращает новое подключение к шине
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        IBus CreateBus(string connectionString);
    }
}