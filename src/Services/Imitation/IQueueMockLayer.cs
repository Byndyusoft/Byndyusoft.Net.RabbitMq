using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;

namespace Byndyusoft.Net.RabbitMq.Services.Imitation
{
    /// <summary>
    ///     Fake bus infrastructure layer for imitating interaction with Rabbit in integration tests
    /// </summary>
    /// <remarks>
    ///     Allows to fake RabbitMQ rather than consume\produce handlers. Also provide abilities:
    ///     - imitate appearance of new message in incoming queue
    ///     - imitate resend messages from error queue
    ///     - collect and verify produced messages 
    /// </remarks>
    public interface IQueueMockLayer
    {
        /// <summary>
        ///     Factory of fake connections to RabbitMq
        /// </summary>
        /// <remarks>
        ///     Should be injected to DI to substitute real <see cref="IBusFactory"/>  
        /// </remarks>
        IBusFactory BusFactory { get; }

        /// <summary>
        ///     Imitates appearance of new message of type in incoming queue
        /// </summary>
        /// <param name="message">Incoming message</param>
        Task ImitateIncoming<TConsumeMessage>(TConsumeMessage message) where TConsumeMessage : class;

        /// <summary>
        ///     Returns single produced of type from internal collection 
        /// </summary>
        /// <typeparam name="TProduceMessage">Produces message type</typeparam>
        TProduceMessage DequeueProduced<TProduceMessage>() where TProduceMessage : class;

        /// <summary>
        ///     Asserts that there are no produced messages af any type left in internal collection
        /// </summary>
        void AssertProducedMessages();
    }
}