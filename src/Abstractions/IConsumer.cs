using System;
using Byndyusoft.AspNetCore.RabbitMq.Models;

namespace Byndyusoft.AspNetCore.RabbitMq.Abstractions
{
    public interface IConsumer : IDisposable
    {
        void Subscribe(params ISubscription[] subscriptions);

        void Listen(Exchange exchange, string queueName);
    }
}