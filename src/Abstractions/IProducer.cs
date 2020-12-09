using System.Collections.Generic;
using System.Threading.Tasks;
using Byndyusoft.AspNetCore.RabbitMq.Models;

namespace Byndyusoft.AspNetCore.RabbitMq.Abstractions
{
    public interface IProducer
    {
        Task PublishAsync<T>(Exchange exchange, string routingKey, T message, Dictionary<string, string> headers = null);
    }
}