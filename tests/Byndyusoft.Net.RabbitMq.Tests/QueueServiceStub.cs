using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;

namespace Sod.Tests.Shared.IntegrationTests.Stubs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using EasyNetQ;
    using EasyNetQ.DI;
    using EasyNetQ.SystemMessages;
    using EasyNetQ.Topology;
    using Moq;
    using Newtonsoft.Json;
    using OpenTracing.Util;
    using RabbitMQ.Client;
    using BasicGetResult = RabbitMQ.Client.BasicGetResult;
    using IConnectionFactory = EasyNetQ.IConnectionFactory;
    using JsonSerializer = EasyNetQ.JsonSerializer;

    /// <summary>
    ///     Класс для имитации работы с очередью сообщений
    /// </summary>
    /// <typeparam name="TProcessMessage">Тип входящих сообщений сервиса</typeparam>
    public class QueueServiceStub<TProcessMessage> where TProcessMessage : class
    {
        private readonly Mock<IBusFactory>? _busServiceMock;
        private readonly Dictionary<Type, Queue<object>> _pushedMessages;
        private readonly Queue<object> _resendMessages;
        private Func<IMessage<TProcessMessage>, MessageReceivedInfo, Task> _processQueueMessage;
        private readonly Mock<IModel> _model;

        /// <summary>
        ///     Фабрика подулючений к шине
        /// </summary>
        public IBusFactory BusService => _busServiceMock.Object;

        public QueueServiceStub()
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

            advancedBusMock.Setup(x => x.Consume(It.IsAny<IQueue>(), It.IsAny<Func<IMessage<TProcessMessage>, MessageReceivedInfo, Task>>()))
                           .Callback((IQueue queue, Func<IMessage<TProcessMessage>, MessageReceivedInfo, Task> processMessageFunc) => _processQueueMessage = processMessageFunc);

            advancedBusMock.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), true, It.IsAny<IMessage>()))
                           .Callback(async (IExchange exchange, string routingKey, bool mandatory, IMessage message) =>
                                         {
                                             CollectPushedMessage(message.GetBody());
                                             if (message.GetBody() is TProcessMessage)
                                             {
                                                 if(routingKey == "resend")
                                                 {
                                                     await ImitateNewMessageReceiving((TProcessMessage) message.GetBody()).ConfigureAwait(false);
                                                 }
                                             }
                                         });


            _model = new Mock<IModel>();
            var connection = new Mock<IConnection>();
            connection.Setup(conn => conn.CreateModel()).Returns(_model.Object);
            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(factory => factory.CreateConnection()).Returns(connection.Object);
            var serviceResolver = new Mock<IServiceResolver>();
            serviceResolver.Setup(resolver => resolver.Resolve<IConnectionFactory>()).Returns(connectionFactory.Object);
            advancedBusMock.SetupGet(bus => bus.Container).Returns(serviceResolver.Object);
            
            var errorQueue = Mock.Of<IQueue>();
            var errorRoutingKey = ".error";
            advancedBusMock.Setup(x => x.QueueDeclareAsync(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false, true, false, false, null, null, null, null, null, null, null))
                           .ReturnsAsync(errorQueue);

            advancedBusMock.Setup(bus => bus.MessageCount(errorQueue))
                           .Returns(() =>
                           {
                               if (_resendMessages != null)
                               {
                                   return (uint)_resendMessages.Count;
                               }

                               return 0;
                           });


            _model.Setup(m => m.BasicGet(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false))
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
                              Type = typeof(TProcessMessage).ToString()
                          },
                          Message = Encoding.UTF8.GetString(jsonSerializer.MessageToBytes(typeof(TProcessMessage), msg))
                      };
                      var body = jsonSerializer.MessageToBytes(typeof(Error), error);
                      var basicResult = new BasicGetResult(1, false, "", errorRoutingKey, 1, Mock.Of<IBasicProperties>(), body);
                      return basicResult;
                  });
        }

        /// <summary>
        ///     Сохраняет отправленное исходящее сообщение во внутренней коллекции для дальнейших проверок
        /// </summary>
        private void CollectPushedMessage(object message)
        {
            var messageType = message.GetType();

            if (_pushedMessages.ContainsKey(messageType) == false)
                _pushedMessages[messageType] = new Queue<object>();

            _pushedMessages[messageType].Enqueue(message);
        }

        /// <summary>
        ///     Имитирует появление входящего сообщения
        /// </summary>
        /// <param name="processMessage">Входящее сообщение</param>
        public async Task ImitateNewMessageReceiving(TProcessMessage processMessage)
        {
            if (_processQueueMessage == null)
                throw new Exception("No subscribers. Queue service was not registered or used wrong type");

            using var scope = GlobalTracer.Instance.BuildSpan(nameof(ImitateNewMessageReceiving)).StartActive();
            try
            {
                await _processQueueMessage(new Message<TProcessMessage>(processMessage), new MessageReceivedInfo());
            }
            catch (Exception)
            {
                _resendMessages.Enqueue(processMessage);
            }
        }

        /// <summary>
        ///     Возвращает все отправленые исходящие сообщения типа
        /// </summary>
        /// <typeparam name="TMessage">Тип исходящего сообщения</typeparam>
        public TMessage GetPushedMessage<TMessage>()
        {
            var messageType = typeof(TMessage);

            if (_pushedMessages.ContainsKey(messageType) == false)
                throw new Exception($"No message of type {messageType} was pushed");

            if (_pushedMessages[messageType].Any() == false)
                throw new Exception($"No more messages of type {messageType} were pushed");

            var pushedMessage = (TMessage)_pushedMessages[messageType].Dequeue();

            return pushedMessage;
        }

        /// <summary>
        ///     Проверяет, что не осталось непрочитанных исходящих сообщений
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