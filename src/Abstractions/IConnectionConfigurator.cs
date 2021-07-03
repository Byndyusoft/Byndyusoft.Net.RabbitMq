namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор подключения к шине RabbitMq
    /// </summary>
    public interface IConnectionConfigurator
    {
        /// <summary>
        ///     Устанавливает строку подключения к шине
        /// </summary>
        ITopologyConfigurator Connection(string connectionString);
    }
}
