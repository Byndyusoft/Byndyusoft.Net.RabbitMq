namespace Byndyusoft.Messaging.RabbitMq.PubSub.Publishing
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRabbitMqMessagePublisher
    {
        Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class;
    }
}