using System.Threading.Tasks;

namespace Byndyusoft.AspNetCore.RabbitMq.Abstractions
{
    public interface IMessageHandler
    {
        Task Handle(string message);
    }
}