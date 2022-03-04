using System;
using System.Collections.Generic;
using EasyNetQ.Topology;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Consuming or producing message pipeline
    /// </summary>
    public class QueuePipeline
    {
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="routingKey">Queue routing key</param>
        /// <param name="queue">Target queue</param>
        /// <param name="exchange">Target exchange</param>
        public QueuePipeline(string routingKey, IQueue queue, IExchange exchange)
        {
            RoutingKey = routingKey;
            Queue = queue;
            Exchange = exchange;
            ProcessMiddlewares = new List<Type>();
            ReturnedMiddlewares = new List<Type>();
        }

        /// <summary>
        ///     Queue routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Target queue
        /// </summary>
        public IQueue Queue { get; }

        /// <summary>
        ///     Target exchange
        /// </summary>
        public IExchange Exchange { get; }

        /// <summary>
        ///     Message producing or consuming middlewares
        /// </summary>
        public IList<Type> ProcessMiddlewares { get; }

        /// <summary>
        ///     Failure pipes for handling errors on message consuming or returned produced messages
        /// </summary>
        public IList<Type> ReturnedMiddlewares { get; }
    }
}