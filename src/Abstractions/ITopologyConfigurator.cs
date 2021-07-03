using System;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    /// <summary>
    ///     Конфигуратор топологии 
    /// </summary>
    public interface ITopologyConfigurator
    {
        /// <summary>
        ///     Добавляет обменник в топологию
        /// </summary>
        /// <param name="exchangeName">Имя обменник</param>
        /// <param name="setupExchange">Делегат для настройки обменника</param>
        ITopologyConfigurator Exchange(string exchangeName, Action<IExchangeConfigurator> setupExchange);
    }
}