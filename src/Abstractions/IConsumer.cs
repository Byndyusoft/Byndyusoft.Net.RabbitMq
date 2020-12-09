using System;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    public interface IConsumer : IDisposable
    {
        void Subscribe(params ISubscription[] subscriptions);

        void Listen(Exchange exchange, string queueName);
    }
}