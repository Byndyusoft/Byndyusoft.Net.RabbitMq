using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Thrift.Protocol.Entities;

namespace Byndyusoft.Net.RabbitMq.Tests
{
    using BasicGetResult = RabbitMQ.Client.BasicGetResult;
    using IConnectionFactory = EasyNetQ.IConnectionFactory;
    using JsonSerializer = EasyNetQ.JsonSerializer;

    /// <summary>
    ///     Fake bus infrastructure layer for imitating interaction with Rabbit in integration tests
    /// </summary>
    public sealed class QueueImitationLayer
    {
        /// <summary>
        ///     Mock of RabbitMq connection factory
        /// </summary>
        private readonly Mock<IBusFactory> _busServiceMock;

        /// <summary>
        ///     All messages were pushed during the session
        /// </summary>
        private readonly Dictionary<Type, Queue<object>> _pushedMessages;

        
        private readonly Dictionary<Type, ISubscribeImitation> _subscribeImitations;
        
        /// <summary>
        ///     Mock of internal EasyNetQ abstraction - AMQP model
        /// </summary>
        private readonly Mock<IModel> _model;

        private Mock<IAdvancedBus> _advancedBusMock;

        /// <summary>
        ///     Factory of fake connections to RabbitMq
        /// </summary>
        /// <remarks>
        ///     Should be injected to DI to substitute real <see cref="IBusFactory"/>  
        /// </remarks>
        public IBusFactory BusService => _busServiceMock.Object;

        public QueueImitationLayer()
        {
            _subscribeImitations = new Dictionary<Type, ISubscribeImitation>();
            _pushedMessages = new Dictionary<Type, Queue<object>>();

            _busServiceMock = new Mock<IBusFactory>();

            var bus = new Mock<IBus>();

            _busServiceMock.Setup(x => x.CreateBus(It.IsAny<RabbitMqConfiguration>()))
                           .Callback((RabbitMqConfiguration cfg) => ConfigureImitations(cfg))
                           .Returns(bus.Object);

            _advancedBusMock = new Mock<IAdvancedBus>();

            bus.Setup(x => x.Advanced)
               .Returns(_advancedBusMock.Object);

            _advancedBusMock.Setup(x => x.Conventions)
                           .Returns(new Conventions(new DefaultTypeNameSerializer()));

            _advancedBusMock.Setup(x => x.ExchangeDeclareAsync(It.IsAny<string>(), It.IsAny<string>(), false, true, false, false, null, false))
                           .ReturnsAsync(new Exchange(nameof(Exchange)));

            _model = new Mock<IModel>();
            var connection = new Mock<IConnection>();
            connection.Setup(conn => conn.CreateModel()).Returns(_model.Object);
            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(factory => factory.CreateConnection()).Returns(connection.Object);
            var serviceResolver = new Mock<IServiceResolver>();
            serviceResolver.Setup(resolver => resolver.Resolve<IConnectionFactory>()).Returns(connectionFactory.Object);
            _advancedBusMock.SetupGet(bus => bus.Container).Returns(serviceResolver.Object);
        }

        private void ConfigureImitations(RabbitMqConfiguration configuration)
        {
            foreach (var consumeConfiguration in configuration.ExchangeConfigurations.SelectMany(ex =>
                ex.Value.ConsumeQueueConfigurations))
            {
                var method =
                    typeof(QueueImitationLayer).GetMethod(nameof(ConfigureConsumeImitations), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    throw new PipelineConfigurationException("Failed to imitate consume pipeline");

                var generic = method.MakeGenericMethod(consumeConfiguration.MessageType);
                generic.Invoke(this, null);
            }

            foreach (var produceConfiguration in configuration.ExchangeConfigurations.SelectMany(ex =>
                ex.Value.ProduceQueueConfigurations))
            {
                var method =
                    typeof(QueueImitationLayer).GetMethod(nameof(ConfigureProduceImitation), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    throw new PipelineConfigurationException("Failed to imitate produce pipeline");

                var generic = method.MakeGenericMethod(produceConfiguration.MessageType);
                generic.Invoke(this, null);
            }
        }

        private void ConfigureProduceImitation<TProduceMessage>() where TProduceMessage : class
        {
            _advancedBusMock.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), true, It.IsAny<EasyNetQ.IMessage>()))
                .Callback(async (IExchange exchange, string routingKey, bool mandatory, EasyNetQ.IMessage message) =>
                {
                    CollectPushedMessage(message.GetBody());
                    if (message.GetBody() is TMessage)
                    {
                        if (routingKey == "resend")
                        {
                            await ImitateNewMessageReceiving(typeof(TProduceMessage), message.GetBody()).ConfigureAwait(false);
                        }
                    }
                });
        }

        private void ConfigureConsumeImitations<TConsumeMessage>() where TConsumeMessage : class
        {
            var imitation = new ConsumeImitation<TConsumeMessage>(_advancedBusMock, _model);
            _subscribeImitations.Add(typeof(TConsumeMessage), imitation);
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
        /// <param name="message">Входящее сообщение</param>
        public Task ImitateNewMessageReceiving<TConsumeMessage>(TConsumeMessage message) where TConsumeMessage : class
        {
            return ImitateNewMessageReceiving(typeof(TConsumeMessage), message);
        }

        /// <summary>
        ///     Имитирует появление входящего сообщения
        /// </summary>
        /// <param name="message">Входящее сообщение</param>
        public async Task ImitateNewMessageReceiving(Type messageType, object message)
        {
            if (_subscribeImitations.TryGetValue(messageType, out var imitation) == false)
                throw new Exception("No subscribers. Queue service was not registered or used wrong type");

            using var scope = GlobalTracer.Instance.BuildSpan(nameof(ImitateNewMessageReceiving)).StartActive();
            await imitation.ImitateNewMessageReceiving(message);
        }

        /// <summary>
        ///     Возвращает все отправленые исходящие сообщения типа
        /// </summary>
        /// <typeparam name="TProduceMessage">Тип исходящего сообщения</typeparam>
        public TMessage GetPushedMessage<TProduceMessage>() where TProduceMessage : class
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

    internal interface ISubscribeImitation
    {
        Task ImitateNewMessageReceiving(object message);
    }

    internal sealed class ConsumeImitation<TSubscribeMessage> : ISubscribeImitation where TSubscribeMessage : class
    {
        private readonly Mock<IAdvancedBus> _advancedBusMock;
        
        /// <summary>
        ///     All messages were resent during the session
        /// </summary>
        private readonly Queue<TSubscribeMessage> _resendMessages;
        
        /// <summary>
        ///     Mock of internal EasyNetQ abstraction - AMQP model
        /// </summary>
        private readonly Mock<IModel> _modelMock;

        private Func<IMessage<TSubscribeMessage>, MessageReceivedInfo, Task> _processQueueMessage;

        public ConsumeImitation(Mock<IAdvancedBus> advancedBusMock, Mock<IModel> modelMock)
        {
            _advancedBusMock = advancedBusMock;
            _modelMock = modelMock;

            _advancedBusMock.Setup(x => x.Consume<TSubscribeMessage>(It.IsAny<IQueue>(), It.IsAny<Func<IMessage<TSubscribeMessage>, MessageReceivedInfo, Task>>()))
                .Callback((IQueue queue, Func<IMessage<TSubscribeMessage>, MessageReceivedInfo, Task> processMessageFunc) => _processQueueMessage = processMessageFunc);

            _modelMock.Setup(m => m.BasicGet(It.Is<string>(e => e.EndsWith(".error")), false))
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
                            Type = typeof(TMessage).ToString()
                        },
                        Message = Encoding.UTF8.GetString(jsonSerializer.MessageToBytes(typeof(TMessage), msg))
                    };
                    var body = jsonSerializer.MessageToBytes(typeof(Error), error);
                    var basicResult = new BasicGetResult(1, false, "", ".error", 1, Mock.Of<IBasicProperties>(), body);
                    return basicResult;
                });

            var errorQueue = Mock.Of<IQueue>();
            var errorRoutingKey = ".error";
            _advancedBusMock.Setup(x => x.QueueDeclareAsync(It.Is<string>(e => e.EndsWith(errorRoutingKey)), false, true, false, false, null, null, null, null, null, null, null))
                .ReturnsAsync(errorQueue);

            _advancedBusMock.Setup(bus => bus.MessageCount(errorQueue))
                .Returns(() =>
                {
                    if (_resendMessages != null)
                    {
                        return (uint)_resendMessages.Count;
                    }

                    return 0;
                });
        }
        
        /// <summary>
        ///     Имитирует появление входящего сообщения
        /// </summary>
        /// <param name="message">Входящее сообщение</param>
        public Task ImitateNewMessageReceiving(object message)
        {
            return ImitateMessageReceiving((TSubscribeMessage)message);
        }

        /// <summary>
        ///     Имитирует появление входящего сообщения
        /// </summary>
        /// <param name="message">Входящее сообщение</param>
        private async Task ImitateMessageReceiving(TSubscribeMessage message)
        {
            if (_processQueueMessage == null)
                throw new Exception("No subscribers. Queue service was not registered or used wrong type");

            using var scope = GlobalTracer.Instance.BuildSpan(nameof(ImitateNewMessageReceiving)).StartActive();
            try
            {
                await _processQueueMessage(new Message<TSubscribeMessage>(message), new MessageReceivedInfo());
            }
            catch (Exception)
            {
                _resendMessages.Enqueue(message);
            }
        }
    }
}