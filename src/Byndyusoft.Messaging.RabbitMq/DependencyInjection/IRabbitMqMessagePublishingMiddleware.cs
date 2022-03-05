using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqMessagePublishingMiddleware
    {
        Task InvokeAsync(RabbitMqMessage message, IRabbitMqMessagePublishingMiddleware next);
    }
}