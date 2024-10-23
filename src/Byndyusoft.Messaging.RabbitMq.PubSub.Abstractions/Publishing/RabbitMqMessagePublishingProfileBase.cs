namespace Byndyusoft.Messaging.RabbitMq.PubSub.Publishing
{
    using System.Net.Http;

    public interface IRabbitMqMessagePublishingProfile
    {
        HttpContent GetMessageContent(object message);
    }

    public abstract class RabbitMqMessagePublishingProfileBase<TMessage> : IRabbitMqMessagePublishingProfile
    {
        protected abstract HttpContent GetMessageContent(TMessage message);

        public HttpContent GetMessageContent(object message) => GetMessageContent((TMessage) message);
    }
}