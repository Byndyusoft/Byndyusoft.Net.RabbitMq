using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    public class SentCounter<TMessage> : IProducePipe<TMessage> where TMessage : class
    {
        public Task<IMessage<TMessage>> Pipe(IMessage<TMessage> message)
        {
            // инкрементим счётчик отправленных
            return Task.FromResult(message);
        }
    }
}