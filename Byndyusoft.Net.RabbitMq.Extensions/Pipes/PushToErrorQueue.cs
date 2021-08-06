using System;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;

namespace Byndyusoft.Net.RabbitMq.Extensions.Pipes
{
    public sealed class PushToErrorQueue<TMessage> : IConsumeErrorPipe<TMessage> where TMessage : class
    {
        public PushToErrorQueue()
        {
            // здесь настраиваем конвенции
        }

        public Task<(TMessage, Exception)> Pipe(TMessage message, Exception e)
        {
            throw e;
        }
    }
}