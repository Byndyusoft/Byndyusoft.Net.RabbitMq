using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using FluentAssertions;
using Xunit;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class QueueServicePublishFunctionalTests : QueueServiceFunctionalTests
    {
        [Fact]
        public async Task Publish_Test()
        {
            //Arrange
            const string testName = nameof(Publish_Test);
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
                ProduceQueueConfigurations =
                {
                    new QueueConfiguration(queueName, routingKey, typeof(Message))
                }
            });

            await _queueService.StartAsync();

            //Act
            var message = new Message
            {
                Content = content
            };
            var headers = new Dictionary<string, string> {{"header", "value"}};
            await _queueService.Publish(message, "key", headers);

            //Assert
            var actual = _rabbit.Get(queue).Should().NotBeNull().And.Subject as IBasicGetResult;
            var actualMessage = new JsonSerializer().BytesToMessage(typeof(Message), actual!.Body) as Message;
            actualMessage!.Content.Should().Be(content);

            var valueBytes = actual.Properties.Headers.Should().ContainSingle(x => x.Key == "header").Which.Value
                .Should().BeOfType<byte[]>().Subject;
            Encoding.UTF8.GetString(valueBytes).Should().Be("value");
        }

        //public async Task Publish_Test2()
        //{
        //    //Arrange
        //    const string testName = nameof(Publish_Test);
        //    const string content = "content";
        //    const string routingKey = "routingKey";
        //    var queueName = $"{testName}_queue";
        //    var exchangeName = $"{testName}_exchange";

        //    using var queue = DeclareQueue(queueName);
        //    using var exchange = DeclareExchange(exchangeName);
        //    await _rabbit.BindAsync(exchange, queue, routingKey,_cancellationToken);
        //    _rabbit.QueuePurge(queue);

        //    var queueService = new Byndyusoft.Messaging.QueueService(_configuration.ConnectionString);

        //    //Act
        //    var message = new QueueMessage()
        //    {
        //        Exchange = exchangeName,
        //        RoutingKey = routingKey,
        //        Content = new StringContent(content)
        //    };
        //    await queueService.PublishAsync(message, _cancellationToken);

        //    //Assert

        //}
    }
}