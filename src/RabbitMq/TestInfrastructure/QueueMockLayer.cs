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
using EasyNetQ.Topology;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Byndyusoft.Net.RabbitMq.TestInfrastructure
{
    using IConnectionFactory = EasyNetQ.IConnectionFactory;

    /// <summary>
    ///     Fake bus infrastructure layer for imitating interaction with Rabbit in integration tests
    /// </summary>
    /// <remarks>
    ///     Allows to fake RabbitMQ rather than consume\produce handlers. Also provide abilities:
    ///     - imitate appearance of new message in incoming queue
    ///     - imitate resend messages from error queue
    ///     - collect and verify produced messages 
    /// </remarks>
    public sealed class QueueMockLayer : IQueueMockLayer
    {
        /// <summary>
        ///     Mock of RabbitMq connection factory
        /// </summary>
        private readonly Mock<IBusFactory> _busServiceMock;

        /// <summary>
        ///     Mock of AMQP API
        /// </summary>
        private readonly Mock<IModel> _model;

        /// <summary>
        ///     Mock of advanced bus API
        /// </summary>
        private readonly Mock<IAdvancedBus> _advancedBusMock;

        /// <summary>
        ///     All messages were pushed during the session
        /// </summary>
        private readonly Dictionary<Type, Queue<object>> _pushedMessages;

        /// <summary>
        ///     Subscribe imitation containers
        /// </summary>
        
        private readonly Dictionary<Type, IConsumeMock> _consumeImitations;

        /// <summary>
        ///     Factory of fake connections to RabbitMq
        /// </summary>
        /// <remarks>
        ///     Should be injected to DI to substitute real <see cref="IBusFactory"/>  
        /// </remarks>
        public IBusFactory BusFactory => _busServiceMock.Object;
        
        /// <summary>
        ///     Ctor
        /// </summary>
        public QueueMockLayer()
        {
            _model = new Mock<IModel>();
            _advancedBusMock = new Mock<IAdvancedBus>();
            _busServiceMock = new Mock<IBusFactory>();
            _consumeImitations = new Dictionary<Type, IConsumeMock>();
            _pushedMessages = new Dictionary<Type, Queue<object>>();

            var bus = new Mock<IBus>();


            _busServiceMock.Setup(x => x.CreateBus(It.IsAny<RabbitMqConfiguration>()))
                           .Callback((RabbitMqConfiguration cfg) => ConfigureImitations(cfg))
                           .Returns(bus.Object);

            bus.Setup(x => x.Advanced)
               .Returns(_advancedBusMock.Object);

            _advancedBusMock.Setup(x => x.Conventions)
                           .Returns(new Conventions(new DefaultTypeNameSerializer()));

            _advancedBusMock.Setup(x => x.ExchangeDeclareAsync(It.IsAny<string>(), It.IsAny<string>(), false, true, false, false, null, false))
                           .ReturnsAsync(new Exchange(nameof(Exchange)));

            var connection = new Mock<IConnection>();
            connection.Setup(conn => conn.CreateModel()).Returns(_model.Object);
            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(factory => factory.CreateConnection()).Returns(connection.Object);
            var serviceResolver = new Mock<IServiceResolver>();
            serviceResolver.Setup(resolver => resolver.Resolve<IConnectionFactory>()).Returns(connectionFactory.Object);
            _advancedBusMock.SetupGet(advancedBus => advancedBus.Container).Returns(serviceResolver.Object);
        }

        /// <summary>
        ///     Configures produce\consume imitations according to topology configuration
        /// </summary>
        /// <param name="configuration">TopologyConfiguration</param>
        private void ConfigureImitations(RabbitMqConfiguration configuration)
        {
            foreach (var consumeConfiguration in configuration.ExchangeConfigurations.SelectMany(ex =>
                ex.Value.ConsumeQueueConfigurations))
            {
                var method =
                    typeof(QueueMockLayer).GetMethod(nameof(ConfigureConsumeImitations), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    throw new PipelineConfigurationException("Failed to imitate consume pipeline");

                var generic = method.MakeGenericMethod(consumeConfiguration.MessageType);
                generic.Invoke(this, null);
            }

            foreach (var produceConfiguration in configuration.ExchangeConfigurations.SelectMany(ex =>
                ex.Value.ProduceQueueConfigurations))
            {
                var method =
                    typeof(QueueMockLayer).GetMethod(nameof(ConfigureProduceImitation), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    throw new PipelineConfigurationException("Failed to imitate produce pipeline");

                var generic = method.MakeGenericMethod(produceConfiguration.MessageType);
                generic.Invoke(this, null);
            }
        }

        /// <summary>
        ///     Configures imitation of producing of message of type
        /// </summary>
        /// <typeparam name="TProduceMessage">Type of producing message</typeparam>
        private void ConfigureProduceImitation<TProduceMessage>() where TProduceMessage : class
        {
            _advancedBusMock.Setup(x => x.PublishAsync(It.IsAny<IExchange>(), It.IsAny<string>(), true, It.IsAny<IMessage>()))
                .Callback(async (IExchange exchange, string routingKey, bool mandatory, IMessage message) =>
                {
                    CollectProduced(message.GetBody());
                    if (message.GetBody() is TProduceMessage)
                    {
                        if (routingKey == "resend")
                        {
                            await ImitateConsuming(typeof(TProduceMessage), message.GetBody()).ConfigureAwait(false);
                        }
                    }
                });
        }

        /// <summary>
        ///     Configures imitation of consuming of message of type
        /// </summary>
        /// <typeparam name="TConsumeMessage">Type of consuming message</typeparam>
        private void ConfigureConsumeImitations<TConsumeMessage>() where TConsumeMessage : class
        {
            var imitation = new ConsumeMock<TConsumeMessage>(_advancedBusMock, _model);
            _consumeImitations.Add(typeof(TConsumeMessage), imitation);
        }

        /// <summary>
        ///     Collects produced message for further assertions
        /// </summary>
        private void CollectProduced(object message)
        {
            var messageType = message.GetType();

            if (_pushedMessages.ContainsKey(messageType) == false)
                _pushedMessages[messageType] = new Queue<object>();

            _pushedMessages[messageType].Enqueue(message);
        }

        /// <summary>
        ///     Imitates appearance of new message of type in incoming queue
        /// </summary>
        /// <param name="message">Incoming message</param>
        public Task ImitateIncoming<TConsumeMessage>(TConsumeMessage message) where TConsumeMessage : class
        {
            return ImitateConsuming(typeof(TConsumeMessage), message);
        }

        /// <summary>
        ///     Imitates appearance of new message of type in incoming queue
        /// </summary>
        /// <param name="messageType">Incoming message type</param>
        /// <param name="message">Incoming message</param>
        private async Task ImitateConsuming(Type messageType, object message)
        {
            if (_consumeImitations.TryGetValue(messageType, out var imitation) == false)
                throw new Exception($"Consumer for messages of type {messageType} wasn't registered");
            
            await imitation.ImitateIncoming(message);
        }

        /// <summary>
        ///     Returns single produced of type from internal collection 
        /// </summary>
        /// <typeparam name="TProduceMessage">Produces message type</typeparam>
        public TProduceMessage DequeueProduced<TProduceMessage>() where TProduceMessage : class
        {
            var messageType = typeof(TProduceMessage);

            if (_pushedMessages.ContainsKey(messageType) == false)
                throw new Exception($"No message of type {messageType} was produced");

            if (_pushedMessages[messageType].Any() == false)
                throw new Exception($"No more messages of type {messageType} were produced");

            var pushedMessage = (TProduceMessage)_pushedMessages[messageType].Dequeue();

            return pushedMessage;
        }

        /// <summary>
        ///     Asserts that there are no produced messages af any type left in internal collection
        /// </summary>
        public void AssertProducedMessages()
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