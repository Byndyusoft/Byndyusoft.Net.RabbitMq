using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;

namespace Byndyusoft.Net.RabbitMq.Services
{
    public sealed class HandledCounter<TMessage> : IConsumePipe<TMessage> where TMessage : class
    {
        public Task<TMessage> Pipe(TMessage message)
        {
            // инкрементим метрику обработанных
            return Task.FromResult<TMessage>(message);
        }
    }
}