using System.Collections.Generic;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Topology
{
    public class ExchangeOptions
    {
        public static ExchangeOptions Default => new();

        /// <summary>
        ///     Type of the exchange.
        /// </summary>
        public ExchangeType Type { get; set; } = ExchangeType.Direct;

        /// <summary>
        ///     Durability of the exchange. Durable exchanges remain active when a server restarts.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        ///     If set, the exchange is deleted when all queues have finished using it.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        ///     Arguments of the exchange.
        /// </summary>
        public Dictionary<string, object> Arguments { get; } = new();

        /// <summary>
        ///     Sets as durable or not. Durable exchanges remain active when a server restarts.
        /// </summary>
        /// <param name="isDurable">The durable flag to set</param>
        /// <returns>ExchangeOptions</returns>
        public ExchangeOptions AsDurable(bool isDurable)
        {
            Durable = isDurable;
            return this;
        }

        /// <summary>
        ///     Sets as autoDelete or not. If set, the exchange is deleted when all queues have finished using it.
        /// </summary>
        /// <param name="isAutoDelete">The autoDelete flag to set</param>
        /// <returns>ExchangeOptions</returns>
        public ExchangeOptions AsAutoDelete(bool isAutoDelete)
        {
            AutoDelete = isAutoDelete;
            return this;
        }

        /// <summary>Sets type of the exchange.</summary>
        /// <param name="type">The type to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public ExchangeOptions WithType(ExchangeType type)
        {
            Type = type;
            return this;
        }

        /// <summary>Sets an argument for exchange declaration</summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>ExchangeOptions</returns>
        public ExchangeOptions WithArgument(string name, object value)
        {
            Preconditions.CheckNotNull(name, nameof(name));

            Arguments[name] = value;
            return this;
        }
    }
}