using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using FluentAssertions;
using Xunit;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class QueueServiceConsumeFunctionalTests : QueueServiceFunctionalTests
    {
        [Fact]
        public async Task Consume_Test()
        {
            //Arrange
            const string testName = nameof(Consume_Test);
            const string content = "content";
            const string routingKey = "routingKey";
            var queueName = $"{testName}_queue";
            var exchangeName = $"{testName}_exchange";

            using var queue = DeclareQueue(queueName);
            using var exchange = DeclareExchange(exchangeName);
            await _rabbit.BindAsync(exchange, queue, routingKey);
            _rabbit.QueuePurge(queue);

            _configuration.ExchangeConfigurations.Add(exchangeName, new ExchangeConfiguration(exchangeName)
            {
                ConsumeQueueConfigurations =
                {
                    new QueueConfiguration(queueName, routingKey, typeof(Message))
                }
            });

            await _queueService.StartAsync();

            //Act
            Message consumedMessage = null!;
            _queueService.Subscribe<Message>((msg) => Task.FromResult(consumedMessage = msg));

            var properties = new MessageProperties {Type = typeof(Message).AssemblyQualifiedName};
            var body = new JsonSerializer().MessageToBytes(typeof(Message), new Message {Content = content});
            await _rabbit.PublishAsync(exchange, routingKey, true, properties, body);

            await Task.Delay(1000);

            //Assert
            consumedMessage.Should().NotBeNull();
            consumedMessage!.Content.Should().Be(content);
        }
    }
}