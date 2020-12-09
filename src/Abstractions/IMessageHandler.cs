using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    public interface IMessageHandler
    {
        Task Handle(string message);
    }
}