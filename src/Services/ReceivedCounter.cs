using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;

namespace Byndyusoft.Net.RabbitMq.Services
{
    public sealed class ReceivedCounter<TMessage> : IConsumePipe<TMessage> where TMessage : class
    {
        public Task<TMessage> Pipe(TMessage message)
        {
            // инкрементнули метрику входящих
            return Task.FromResult<TMessage>(message);
        }
    }
}
