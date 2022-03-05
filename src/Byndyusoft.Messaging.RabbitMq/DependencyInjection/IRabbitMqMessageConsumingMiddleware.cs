using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqMessageConsumingMiddleware
    {
        Task InvokeAsync(RabbitMqConsumedMessage message, IRabbitMqMessageConsumingMiddleware next);
    }
}