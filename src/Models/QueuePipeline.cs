using System;
using System.Collections.Generic;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ.Topology;

namespace Byndyusoft.Net.RabbitMq.Models
{
    /// <summary>
    ///     Consuming or producing message pipeline
    /// </summary>
    public class QueuePipeline
    {
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
        ///     Message processing wrappers
        /// </summary>
        public IList<Type> ProcessWrappers { get; }

        /// <summary>
        ///     Failure pipes for handling errors on message consuming or returned produced messages
        /// </summary>
        public IList<Type> FailurePipes { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="routingKey">Queue routing key</param>
        /// <param name="queue">Target queue</param>
        /// <param name="exchange">Target exchange</param>
        public QueuePipeline(string routingKey, IQueue queue, IExchange exchange)
        {
            RoutingKey = routingKey;
            Queue = queue;
            Exchange = exchange;
            ProcessWrappers = new List<Type>();
            FailurePipes = new List<Type>();
        }
    }

}