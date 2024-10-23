namespace Byndyusoft.Messaging.RabbitMq.PubSub.Subscribing
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRabbitMqMessageHandler
    {
        Task<ConsumeResult> Handle(HttpContent messageContent, CancellationToken token);
    }

    public abstract class RabbitMqMessageHandlerBase<TMessage> : IRabbitMqMessageHandler where TMessage : class
    {
        protected abstract Task Handle(TMessage message, CancellationToken token);

        public async Task<ConsumeResult> Handle(HttpContent messageContent, CancellationToken token)
        {
            await Handle(await messageContent.ReadAsAsync<TMessage>(token), token);
            return ConsumeResult.Ack;
        }
    }
}