using System;
using System.Collections.Generic;
using Byndyusoft.Net.RabbitMq.Services;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Конфигурация очередей
    /// </summary>
    public sealed class RabbitMqConfiguration
    {
        /// <summary>
        ///     Строка подключения к RabbitMq
        /// </summary>
        public string  ConnectionString { get; set; }

        /// <summary>
        ///     Настройки обменников
        /// </summary>
        public Dictionary<string, ExchangeConfiguration> ExchangeConfigurations { get; }

        /// <summary>
        ///     Ctor
        /// </summary>
        public RabbitMqConfiguration()
        {
            ExchangeConfigurations = new Dictionary<string, ExchangeConfiguration>();
        }

        /// <summary>
        ///      Добавляет настройки обменника
        /// </summary>
        /// <param name="exchangeName"></param>
        public void AddExchange(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            if (ExchangeConfigurations.ContainsKey(exchangeName))
            {
                throw new Exception($"Exchange {exchangeName} has been already added");
            }

            ExchangeConfigurations.Add(exchangeName, new ExchangeConfiguration(exchangeName));
        }
    }

    /// <summary>
    ///     Конфигурация обменника
    /// </summary>
    public sealed class ExchangeConfiguration
    {
        /// <summary>
        ///     Имя обменника
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        ///     Настройки входящих очередей
        /// </summary>
        public HashSet<QueueConfiguration> ConsumeQueueConfigurations { get; }

        /// <summary>
        ///     Настройки исходящих очередей
        /// </summary>
        public HashSet<QueueConfiguration> ProduceQueueConfigurations { get; }


        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="exchangeName">Имя обменника</param>
        public ExchangeConfiguration(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            ExchangeName = exchangeName;
            ConsumeQueueConfigurations = new HashSet<QueueConfiguration>();
            ProduceQueueConfigurations = new HashSet<QueueConfiguration>();
        }
    }
}