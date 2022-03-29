using System.Threading.Tasks;

namespace Byndyusoft.Net.RabbitMq.TestInfrastructure
{
    /// <summary>
    ///     Mock for imitating incoming messages
    /// </summary>
    internal interface IConsumeMock
    {
        /// <summary>
        ///     Imitates appearance of new message in incoming queue
        /// </summary>
        /// <param name="message">Incoming message</param>
        Task ImitateIncoming(object message);
    }
}