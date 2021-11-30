using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using EasyNetQ.DI;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using Moq;
using Newtonsoft.Json;
using OpenTracing.Util;
using RabbitMQ.Client;

namespace Byndyusoft.Net.RabbitMq.Extensions.Testability
{
    using BasicGetResult = RabbitMQ.Client.BasicGetResult;
    using IConnectionFactory = EasyNetQ.IConnectionFactory;
    using JsonSerializer = EasyNetQ.JsonSerializer;

    /// <summary>
    ///     Fake layer for profiling incoming and outgoing message in tests 
    /// </summary>
    /// <typeparam name="TIncomingMessage">MessageType</typeparam>
    public sealed class BusFakeLayer<TIncomingMessage> where TIncomingMessage : class
    {
        /// <summary>
        ///     Provider of fake connection to bus
        /// </summary>
        private readonly Mock<IBusFactory> _busServiceMock;

        /// <summary>
        ///     Messages that were published during the sessions\test
        /// </summary>
        private readonly Dictionary<Type, Queue<object>> _pushedMessages;

        /// <summary>
        ///     Messages that were resent during the sessions\test
        /// </summary>
        private readonly Queue<object> _resendMessages;

        /// <summary>
        /// 
        /// </summary>
        private Func<IMessage<TIncomingMessage>, MessageReceivedInfo, Task> _processQueueMessage;

        /// <summary>
        ///     Provider of fake connection to bus
        /// </summary>
        public IBusFactory BusService => _busServiceMock.Object;

        /// <summary>
        ///     Ctor
        /// </summary>
        public BusFakeLayer()
        {
            _pushedMessages = new Dictionary<Type, Queue<object>>();
            _resendMessages = new Queue<object>();

            _busServiceMock = new Mock<IBusFactory>();

            var bus = new Mock<IBus>();

            _busServiceMock.Setup(x => x.CreateBus(It.IsAny<RabbitMqConfiguration>()))
                           .Returns(bus.Object);

            var advancedBusMock = new Mock<IAdvancedBus>();

            bus.Setup(x => x.Advanced)
               .Returns(advancedBusMock.Object);

            advancedBusMock.Setup(x => x.Conventions)
                           .Returns(new Conventions(new DefaultTypeNameSerializer()));

            advancedBusMock.Setup(x => x.ExchangeDeclareAsync(It.IsAny<string>(), It.IsAny<string>(), false, true, false, false, null, false))
                           .ReturnsAsync(new Exchange(nameof(Exchange)));

            advancedBusMock.Setup(x => x.Consume(It.IsAny<IQueue>(), It.IsAny<Func<IMessage<TIncomingMessage>, MessageReceivedInfo, Task>>()))
                           .Callback((IQueue queue, Func<IMessage<TIncomingMessage>, MessageReceivedInfo, Task> processMessageFunc) => _processQueueMessage = processMessageFunc);

            advancedBusMock.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), true, It.IsAny<IMessage>()))
                           .Callback(async (IExchange exchange, string routingKey, bool mandatory, IMessage message) =>
                                         {
                                             QueuePublishedMessage(message.GetBody());
                                             if (message.GetBody() is TIncomingMessage)
                                             {
                                                 if(routingKey == "resend")
                                                 {
                                                     await ImitateIncomingMessage((TIncomingMessage) message.GetBody());
                                                 }
                                             }
                                         });


            var model = new Mock<IModel>();
            var connection = new Mock<IConnection>();
            connection.Setup(conn => conn.CreateModel()).Returns(model.Object);
            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(factory => factory.CreateConnection()).Returns(connection.Object);
            var serviceResolver = new Mock<IServiceResolver>();
            serviceResolver.Setup(resolver => resolver.Resolve<IConnectionFactory>()).Returns(connectionFactory.Object);
            advancedBusMock.SetupGet(b => b.Container).Returns(serviceResolver.Object);
            
            var errorQueue = Mock.Of<IQueue>();
            var errorRoutingKey = ".error";
            advancedBusMock.Setup(x => x.QueueDeclareAsync(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false, true, false, false, null, null, null, null, null, null, null))
                           .ReturnsAsync(errorQueue);

            advancedBusMock.Setup(b => b.MessageCount(errorQueue))
                           .Returns(() =>
                           {
                               if (_resendMessages != null)
                               {
                                   return (uint)_resendMessages.Count;
                               }

                               return 0;
                           });


            model.Setup(m => m.BasicGet(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false))
                  .Returns(() =>
                  {
                      if (_resendMessages == null)
                          return null;

                      var msg = _resendMessages.Dequeue();

                      var jsonSerializer = new JsonSerializer();
                      var error = new Error()
                      {
                          RoutingKey = "resend",
                          BasicProperties = new MessageProperties
                          {
                              Type = typeof(TIncomingMessage).ToString()
                          },
                          Message = Encoding.UTF8.GetString(jsonSerializer.MessageToBytes(typeof(TIncomingMessage), msg))
                      };
                      var body = jsonSerializer.MessageToBytes(typeof(Error), error);
                      var basicResult = new BasicGetResult(1, false, "", errorRoutingKey, 1, Mock.Of<IBasicProperties>(), body);
                      return basicResult;
                  });
        }

        /// <summary>
        ///     Saves published message into internal collection
        /// </summary>
        private void QueuePublishedMessage(object message)
        {
            var messageType = message.GetType();

            if (_pushedMessages.ContainsKey(messageType) == false)
                _pushedMessages[messageType] = new Queue<object>();

            _pushedMessages[messageType].Enqueue(message);
        }

        /// <summary>
        ///     Imitate arriving of new incoming message for subscription
        /// </summary>
        /// <param name="message">Incoming message</param>
        public async Task ImitateIncomingMessage(TIncomingMessage message)
        {
            if (_processQueueMessage == null)
                throw new Exception("No subscribers. Queue service was not registered or used wrong type");

            using var scope = GlobalTracer.Instance.BuildSpan(nameof(ImitateIncomingMessage)).StartActive();
            try
            {
                await _processQueueMessage(new Message<TIncomingMessage>(message), new MessageReceivedInfo());
            }
            catch (Exception)
            {
                _resendMessages.Enqueue(message);
            }
        }

        /// <summary>
        ///     Removes the message at the head of the queue of published message and returns it
        /// </summary>
        /// <typeparam name="TOutgoingMessage">Outgoing message type</typeparam>
        public TOutgoingMessage DequeuePublishedMessage<TOutgoingMessage>()
        {
            var messageType = typeof(TOutgoingMessage);

            if (_pushedMessages.ContainsKey(messageType) == false)
                throw new Exception($"No message of type {messageType} was pushed");

            if (_pushedMessages[messageType].Any() == false)
                throw new Exception($"No more messages of type {messageType} were pushed");

            var pushedMessage = (TOutgoingMessage)_pushedMessages[messageType].Dequeue();

            return pushedMessage;
        }

        /// <summary>
        ///     Verifies that internal queue of published messages is empty
        /// </summary>
        public void VerifyNoOthersPushes()
        {
            var verifyExceptions = new StringBuilder();

            foreach (var queue in _pushedMessages)
                if (queue.Value.Any())
                    verifyExceptions.AppendLine($"Not verified {queue.Value.Count} messages in queue {queue.Key}. Messages: {JsonConvert.SerializeObject(queue.Value.ToArray())}");

            if (verifyExceptions.Length > 0)
                throw new Exception(verifyExceptions.ToString());
        }
    }
}