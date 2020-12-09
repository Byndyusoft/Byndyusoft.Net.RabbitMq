using System.Collections.Generic;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Models;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    public interface IProducer
    {
        Task PublishAsync<T>(Exchange exchange, string routingKey, T message, Dictionary<string, string> headers = null);
    }
}