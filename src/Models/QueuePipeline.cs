using System.Collections.Generic;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ.Topology;

namespace Byndyusoft.Net.RabbitMq.Services
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
        }
    }

    /// <summary>
    ///     Producing message pipeline
    /// </summary>
    public class ProducingQueuePipeline : QueuePipeline
    {
        /// <summary>
        ///     Message processing wrappers
        /// </summary>
        public IList<IProduceWrapper> ProcessWrappers { get; }

        /// <summary>
        ///     Failure pipes for handling errors on message consuming or returned produced messages
        /// </summary>
        public IList<IProducePipe> FailurePipes { get; }

        public ProducingQueuePipeline(string routingKey, IQueue queue, IExchange exchange) : base(routingKey, queue, exchange)
        {
            ProcessWrappers = new List<IProduceWrapper>();
            FailurePipes = new List<IProducePipe>();
        }
    }

    /// <summary>
    ///     Producing message pipeline
    /// </summary>
    public class ConsumingQueuePipeline : QueuePipeline
    {
        /// <summary>
        ///     Message processing wrappers
        /// </summary>
        public IList<IConsumeWrapper> ProcessWrappers { get; }

        /// <summary>
        ///     Failure pipes for handling errors on message consuming or returned produced messages
        /// </summary>
        public IList<IConsumePipe> FailurePipes { get; }

        public ConsumingQueuePipeline(string routingKey, IQueue queue, IExchange exchange) : base(routingKey, queue, exchange)
        {
            ProcessWrappers = new List<IConsumeWrapper>();
            FailurePipes = new List<IConsumePipe>();
        }
    }
}