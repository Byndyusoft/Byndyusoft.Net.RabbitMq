using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using Moq;
using RabbitMQ.Client;
using BasicGetResult = RabbitMQ.Client.BasicGetResult;

namespace Byndyusoft.Net.RabbitMq.TestInfrastructure
{
    /// <summary>
    ///     Mock for imitating incoming messages
    /// </summary>
    internal sealed class ConsumeMock<TConsumeMessage> : IConsumeMock where TConsumeMessage : class
    {
        /// <summary>
        ///     All messages were resent during the session
        /// </summary>
        private readonly Queue<TConsumeMessage> _resendMessages;

        /// <summary>
        ///     Message consuming delegate
        /// </summary>
        private Func<IMessage<TConsumeMessage>, MessageReceivedInfo, Task>? _consumePipeline;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="advancedBusMock">Mock of advanced bus API</param>
        /// <param name="modelMock">Mock of AMQP API</param>
        public ConsumeMock(Mock<IAdvancedBus> advancedBusMock, Mock<IModel> modelMock)
        {
            _resendMessages = new Queue<TConsumeMessage>();

            advancedBusMock.Setup(x => x.Consume(It.IsAny<IQueue>(), It.IsAny<Func<IMessage<TConsumeMessage>, MessageReceivedInfo, Task>>()))
                .Callback((IQueue queue, Func<IMessage<TConsumeMessage>, MessageReceivedInfo, Task> processMessageFunc) => _consumePipeline = processMessageFunc);

            modelMock.Setup(m => m.BasicGet(It.Is<string>(e => e.EndsWith(".error")), false))
                .Returns(() =>
                {
                    if(_resendMessages.TryDequeue(out var msg) == false)
                        return null;

                    var jsonSerializer = new JsonSerializer();
                    var error = new Error
                    {
                        RoutingKey = "resend",
                        BasicProperties = new MessageProperties
                        {
                            Type = typeof(TConsumeMessage).ToString()
                        },
                        Message = Encoding.UTF8.GetString(jsonSerializer.MessageToBytes(typeof(TConsumeMessage), msg))
                    };
                    var body = jsonSerializer.MessageToBytes(typeof(Error), error);
                    var basicResult = new BasicGetResult(1, false, "", ".error", 1, Mock.Of<IBasicProperties>(), body);
                    return basicResult;
                });

            var errorQueue = Mock.Of<IQueue>();
            var errorRoutingKey = ".error";
            advancedBusMock.Setup(x => x.QueueDeclareAsync(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false, true, false, false, null, null, null, null, null, null, null))
                .ReturnsAsync(errorQueue);

            advancedBusMock.Setup(bus => bus.MessageCount(errorQueue)).Returns(() => (uint)_resendMessages.Count);
        }

        /// <summary>
        ///     Imitates appearance of new message in incoming queue
        /// </summary>
        /// <param name="message">Incoming message</param>
        public Task ImitateIncoming(object message)
        {
            return Imitate((TConsumeMessage)message);
        }

        /// <summary>
        ///     Imitates appearance of new message in incoming queue
        /// </summary>
        /// <param name="message">Incoming message</param>
        private async Task Imitate(TConsumeMessage message)
        {
            if (_consumePipeline == null)
                throw new Exception($"Consumer for messages of type {typeof(TConsumeMessage)} wasn't registered");

            try
            {
                await _consumePipeline(new Message<TConsumeMessage>(message), new MessageReceivedInfo());
            }
            catch (Exception)
            {
                _resendMessages.Enqueue(message);
            }
        }
    }
}