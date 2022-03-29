using System;
using System.Threading;
using Byndyusoft.Net.RabbitMq.Models;
using Byndyusoft.Net.RabbitMq.Services;
using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenTracing;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public abstract class QueueServiceFunctionalTests : IDisposable
    {
        private readonly IBus _bus;
        protected readonly CancellationToken _cancellationToken = new();
        protected readonly RabbitMqConfiguration _configuration;
        protected readonly QueueService _queueService;
        protected readonly IAdvancedBus _rabbit;

        public QueueServiceFunctionalTests()
        {
            Mock<ITracer> tracerMock = new();
            var span = Mock.Of<ISpan>();
            var spanBuilder = Mock.Of<ISpanBuilder>(builder => builder.Start() == span);
            tracerMock.SetupGet(tracer => tracer.ActiveSpan).Returns(span);
            tracerMock.Setup(tracer => tracer.BuildSpan(It.IsAny<string>())).Returns(spanBuilder);

            var connectionString = "host=localhost";

            _configuration = new RabbitMqConfiguration {ConnectionString = connectionString};

            var busFactory = new BusFactory();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            _queueService = new QueueService(busFactory, _configuration, serviceProvider);

            _bus = RabbitHutch.CreateBus(connectionString);
            _rabbit = _bus.Advanced;
        }

        public void Dispose()
        {
            _bus.Dispose();
            _queueService.Dispose();
        }

        public DisposableExchange DeclareExchange(string name, string type = ExchangeType.Direct)
        {
            var exchange = _rabbit.ExchangeDeclare(name, type);
            return new DisposableExchange(_rabbit, exchange);
        }

        public DisposableQueue DeclareQueue(string name)
        {
            var queue = _rabbit.QueueDeclare(name);
            return new DisposableQueue(_rabbit, queue);
        }
    }
}