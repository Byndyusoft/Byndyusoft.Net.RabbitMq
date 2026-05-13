using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.Tests.Functional.Models;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class RabbitMqClientFunctionalTests : IDisposable
    {
        private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
        private readonly IRabbitMqClient _client;
        private readonly RabbitMqClientOptions _options;
        private readonly IAdvancedBus _rabbit;

        public RabbitMqClientFunctionalTests()
        {
            var connectionString = "host=localhost;username=guest;password=guest";
            _options = new RabbitMqClientOptions();

            _client = new ServiceCollection()
                .AddRabbitMqClient(connectionString)
                .BuildServiceProvider()
                .GetRequiredService<IRabbitMqClientFactory>()
                .CreateClient();

            _rabbit = CreateBus(connectionString).Advanced;
        }

        private IBus CreateBus(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddEasyNetQ(connectionString);
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IBus>();
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task PublishToQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(PublishToQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            var serializationOptions = new JsonSerializerOptions();
            var data = new Message {Content = "content"};

            // act
            await using var message = new RabbitMqMessage();
            message.Mandatory = true;
            message.Persistent = true;
            message.Exchange = null;
            message.RoutingKey = queueName;
            message.Content = JsonContent.Create(data, options: serializationOptions);
            message.Properties = new RabbitMqMessageProperties
            {
                ContentType = "type/subtype",
                Priority = 1,
                Type = "type",
                ContentEncoding = "contentEncoding",
                CorrelationId = "correlationId",
                Expiration = TimeSpan.FromMinutes(1),
                AppId = "appId",
                MessageId = "messageId",
                ReplyTo = "replyTo",
                Timestamp = DateTime.UtcNow,
                UserId = "guest"
            };
            message.Headers = new RabbitMqMessageHeaders
            {
                {"key", "value"}
            };
            await _client.PublishMessageAsync(message, _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // assert
            await using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync(_cancellationToken);

            pullingResult.IsAvailable.Should().BeTrue();
            JsonSerializer.Deserialize<Message>(pullingResult.Body.ToArray()).Should().BeEquivalentTo(data);
            pullingResult.Properties.Headers["key"].Should().BeOfType<byte[]>().Subject.Should()
                .BeEquivalentTo(Encoding.UTF8.GetBytes("value"));
            CheckProperties(pullingResult.Properties, message.Properties);
            pullingResult.ReceivedInfo.Queue.Should().Be(queueName);
            pullingResult.ReceivedInfo.RoutingKey.Should().Be(queueName);
        }

        [Fact]
        public async Task PublishToExchange_Test()
        {
            // arrange
            var routingKey = "routingKey";
            var exchangeName = $"{nameof(PublishToQueue_Test)}.exchange";
            var queueName = $"{nameof(PublishToQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await using var exchange = await ExchangeDeclareAsync(exchangeName);
            await _rabbit.BindAsync(new Exchange(exchangeName), new Queue(queueName), routingKey,
                CancellationToken.None);

            var serializationOptions = new JsonSerializerOptions();
            var data = new Message {Content = "content"};

            // act
            await using var message = new RabbitMqMessage();
            message.Mandatory = true;
            message.Persistent = true;
            message.Exchange = exchangeName;
            message.RoutingKey = routingKey;
            message.Content = JsonContent.Create(data, options: serializationOptions);
            message.Properties = new RabbitMqMessageProperties
            {
                ContentType = "type/subtype",
                Priority = 1,
                Type = "type",
                ContentEncoding = "contentEncoding",
                CorrelationId = "correlationId",
                Expiration = TimeSpan.FromMinutes(1),
                AppId = "appId",
                MessageId = "messageId",
                ReplyTo = "replyTo",
                Timestamp = DateTime.UtcNow,
                UserId = "guest"
            };
            message.Headers = new RabbitMqMessageHeaders
            {
                {"key", "value"}
            };
            await _client.PublishMessageAsync(message, _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // assert
            await using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync(_cancellationToken);

            pullingResult.IsAvailable.Should().BeTrue();

            pullingResult.ReceivedInfo.Queue.Should().Be(queueName);
            pullingResult.ReceivedInfo.Exchange.Should().Be(exchangeName);
            pullingResult.ReceivedInfo.RoutingKey.Should().Be(routingKey);
            JsonSerializer.Deserialize<Message>(pullingResult.Body.ToArray()).Should().BeEquivalentTo(data);
            pullingResult.Properties.Headers["key"].Should().BeOfType<byte[]>().Subject.Should()
                .BeEquivalentTo(Encoding.UTF8.GetBytes("value"));
            pullingResult.Properties.ContentType.Should().Be(message.Properties.ContentType);
            pullingResult.Properties.Priority.Should().Be(message.Properties.Priority);
            pullingResult.Properties.Type.Should().Be(message.Properties.Type);
            pullingResult.Properties.ContentEncoding.Should().Be(message.Properties.ContentEncoding);
            pullingResult.Properties.CorrelationId.Should().Be(message.Properties.CorrelationId);
            pullingResult.Properties.Expiration.Should().Be(message.Properties.Expiration);
            pullingResult.Properties.AppId.Should().Be(message.Properties.AppId);
            pullingResult.Properties.MessageId.Should().Be(message.Properties.MessageId);
            pullingResult.Properties.ReplyTo.Should().Be(message.Properties.ReplyTo);
            pullingResult.Properties.Timestamp.Should()
                .Be(new DateTimeOffset(message.Properties.Timestamp.Value).ToUnixTimeMilliseconds());
            pullingResult.Properties.UserId.Should().Be(message.Properties.UserId);
        }

        [Fact]
        public async Task Get_NoMessage_Test()
        {
            // arrange
            var queueName = $"{nameof(Get_NoMessage_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            // act
            await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

            // assert
            message.Should().BeNull();
        }

        [Fact]
        public async Task Get_Test()
        {
            // arrange
            var queueName = $"{nameof(Get_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            var data = new Message {Content = "content"};
            var properties = new MessageProperties
            {
                ContentType = "type/subtype",
                Priority = 1,
                Type = "type",
                ContentEncoding = "contentEncoding",
                CorrelationId = "correlationId",
                Expiration = TimeSpan.FromMilliseconds(10000),
                AppId = "appId",
                MessageId = "messageId",
                ReplyTo = "replyTo",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UserId = "guest",
                Headers = new Dictionary<string, object>
                {
                    {"key", "value"}
                }
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, properties, body, cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

            message.Should().NotBeNull();
            var json = await message!.Content.ReadFromJsonAsync<Message>(cancellationToken: _cancellationToken);
            json.Should().BeEquivalentTo(data);
            message.Headers["key"].Should().Be("value");

            CheckProperties(message.Properties, properties);
        }

        [Fact]
        public async Task CompleteMessage_Ack_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_Ack_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            await _rabbit.PublishAsync(Exchange.Default, queueName, true, new MessageProperties(),
                Array.Empty<byte>(), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            {
                await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

                // act
                await _client.CompleteMessageAsync(message!, ConsumeResult.Ack, _cancellationToken);
            }

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken);
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CompleteMessage_RejectWithoutRequeue_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_RejectWithoutRequeue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            await _rabbit.PublishAsync(Exchange.Default, queueName, true, new MessageProperties(),
                Array.Empty<byte>(), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            {
                await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

                // act
                await _client.CompleteMessageAsync(message!, ConsumeResult.RejectWithoutRequeue, _cancellationToken);
            }

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken);
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CompleteMessage_RejectWithRequeue_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_RejectWithRequeue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            var body = new byte[] {1, 2, 3};
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, properties, new ReadOnlyMemory<byte>(body), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            {
                await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

                // act
                await _client.CompleteMessageAsync(message!, ConsumeResult.RejectWithRequeue, _cancellationToken);
            }

            // assert
            await using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync(_cancellationToken);

            pullingResult.IsAvailable.Should().BeTrue();
            pullingResult.Body.ToArray().Should().BeEquivalentTo(body);
            pullingResult.Properties.MessageId.Should().Be(properties.MessageId);

            (await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken)).MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CompleteMessage_Error_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_Error_Test)}.queue";
            var errorQueueName = _options.NamingConventions.ErrorQueueName(queueName);
            await using var queue = await QueueDeclareAsync(queueName);

            var body = new byte[] {1, 2, 3};
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, properties, body, cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            {
                await using var message = await _client.GetMessageAsync(queueName, _cancellationToken);

                // act
                await _client.CompleteMessageAsync(message!, ConsumeResult.Error(), _cancellationToken);
            }

            await WaitForMessageAsync(errorQueueName, TimeSpan.FromSeconds(5));

            // assert
            await using var consumer = _rabbit.CreatePullingConsumer(new Queue(errorQueueName));
            var pullingResult = await consumer.PullAsync(_cancellationToken);

            pullingResult.IsAvailable.Should().BeTrue();
            pullingResult.Body.ToArray().Should().BeEquivalentTo(body);
            pullingResult.Properties.MessageId.Should().Be(properties.MessageId);

            (await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken)).MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(CreateQueue_Test)}.queue";

            // act
            await _client.CreateQueueAsync(queueName, QueueOptions.Default, _cancellationToken);

            // assert
            using var scope = new AssertionScope();
            (await QueueExistsAsync(queueName)).Should().BeTrue();

            // cleanup
            await _rabbit.QueueDeleteAsync(queueName, cancellationToken: _cancellationToken);
        }

        [Fact]
        public async Task QueueExists_True_Test()
        {
            // arrange
            var queueName = $"{nameof(QueueExists_True_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            // act
            var result = await _client.QueueExistsAsync(queueName, _cancellationToken);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task QueueExists_False_Test()
        {
            // arrange
            var queueName = $"{nameof(QueueExists_False_Test)}.queue";

            // act
            var result = await _client.QueueExistsAsync(queueName, _cancellationToken);

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(DeleteQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            // act
            await _client.DeleteQueueAsync(queueName, cancellationToken: _cancellationToken);

            // assert
            (await QueueExistsAsync(queueName)).Should().BeFalse();
        }

        [Fact]
        public async Task PurgeQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(PurgeQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, new MessageProperties(),
                Array.Empty<byte>(), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            await _client.PurgeQueueAsync(queueName, _cancellationToken);

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken);
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task GetQueueMessageCount_Test()
        {
            // arrange
            var queueName = $"{nameof(GetQueueMessageCount_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, new MessageProperties(),
                Array.Empty<byte>(), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            var messageCount = await _client.GetQueueMessageCountAsync(queueName, _cancellationToken);

            // assert
            messageCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateExchange_Test()
        {
            // arrange
            var exchangeName = $"{nameof(CreateExchange_Test)}.exchange";

            // act
            await _client.CreateExchangeAsync(exchangeName, ExchangeOptions.Default, _cancellationToken);

            // assert
            using var scope = new AssertionScope();
            (await ExchangeExistsAsync(exchangeName)).Should().BeTrue();

            // cleanup
            await _rabbit.ExchangeDeleteAsync(new Exchange(exchangeName), cancellationToken: _cancellationToken);
        }

        [Fact]
        public async Task ExchangeExists_True_Test()
        {
            // arrange
            var exchangeName = $"{nameof(ExchangeExists_True_Test)}.exchange";
            await using var exchange = await ExchangeDeclareAsync(exchangeName);

            // act
            var result = await _client.ExchangeExistsAsync(exchangeName, _cancellationToken);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExchangeExists_False_Test()
        {
            // arrange
            var exchangeName = $"{nameof(ExchangeExists_False_Test)}.exchange";

            // act
            var result = await _client.ExchangeExistsAsync(exchangeName, _cancellationToken);

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteExchange_Test()
        {
            // arrange
            var exchangeName = $"{nameof(DeleteExchange_Test)}.exchange";
            await using var exchange = await ExchangeDeclareAsync(exchangeName);

            // act
            await _client.DeleteExchangeAsync(exchangeName, cancellationToken: _cancellationToken);

            // assert
            (await ExchangeExistsAsync(exchangeName)).Should().BeFalse();
        }

        [Fact]
        public async Task BindQueue_Test()
        {
            // arrange
            var routingKey = "routingKey";
            var exchangeName = $"{nameof(BindQueue_Test)}.exchange";
            var queueName = $"{nameof(BindQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await using var exchange = await ExchangeDeclareAsync(exchangeName);

            // act
            await _client.BindQueueAsync(exchangeName, routingKey, queueName, _cancellationToken);

            // assert
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, new MessageProperties(),
                Array.Empty<byte>(), cancellationToken: _cancellationToken);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            (await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken)).MessagesCount.Should().Be(1);
        }

        [Fact]
        public async Task Subscribe_Test()
        {
            // arrange
            var queueName = $"{nameof(Subscribe_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            ReceivedRabbitMqMessage? receivedMessage = null;

            using var consumer = _client.Subscribe(queueName, (message, _) =>
            {
                receivedMessage = message;
                return Task.FromResult(ConsumeResult.Ack);
            }).Start();

            // act
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.Default, queueName, true, properties, Array.Empty<byte>(), cancellationToken: _cancellationToken);

            // assert
            await WaitForAsync(() => receivedMessage is not null, TimeSpan.FromSeconds(5));
            receivedMessage.Should().NotBeNull();
            receivedMessage!.Properties.MessageId.Should().Be(properties.MessageId);
        }

        private async Task<bool> QueueExistsAsync(string queueName)
        {
            try
            {
                await _rabbit.QueueDeclarePassiveAsync(queueName, _cancellationToken);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason?.ReplyCode == 404)
            {
                return false;
            }
        }

        private async Task<bool> ExchangeExistsAsync(string exchangeName)
        {
            try
            {
               await _rabbit.ExchangeDeclarePassiveAsync(exchangeName, _cancellationToken);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason?.ReplyCode == 404)
            {
                return false;
            }
        }

        private void CheckProperties(RabbitMqMessageProperties actual, MessageProperties expected)
        {
            actual.ContentType.Should().Be(expected.ContentType);
            actual.Priority.Should().Be(expected.Priority);
            actual.Type.Should().Be(expected.Type);
            actual.ContentEncoding.Should().Be(expected.ContentEncoding);
            actual.CorrelationId.Should().Be(expected.CorrelationId);
            actual.Expiration.Should().Be(expected.Expiration);
            actual.AppId.Should().Be(expected.AppId);
            actual.MessageId.Should().Be(expected.MessageId);
            actual.ReplyTo.Should().Be(expected.ReplyTo);
            actual.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(expected.Timestamp).DateTime);
            actual.UserId.Should().Be(expected.UserId);
        }

        private void CheckProperties(MessageProperties actual, RabbitMqMessageProperties expected)
        {
            actual.ContentType.Should().Be(expected.ContentType);
            actual.Priority.Should().Be(expected.Priority);
            actual.Type.Should().Be(expected.Type);
            actual.ContentEncoding.Should().Be(expected.ContentEncoding);
            actual.CorrelationId.Should().Be(expected.CorrelationId);
            actual.Expiration.Should().Be(expected.Expiration);
            actual.AppId.Should().Be(expected.AppId);
            actual.MessageId.Should().Be(expected.MessageId);
            actual.ReplyTo.Should().Be(expected.ReplyTo);
            actual.Timestamp.Should().Be(new DateTimeOffset(expected.Timestamp!.Value).ToUnixTimeMilliseconds());
            actual.UserId.Should().Be(expected.UserId);
        }

        private Task WaitForMessageAsync(string queueName, TimeSpan timeout)
        {
            return WaitForAsync(async () => (await _rabbit.GetQueueStatsAsync(queueName, _cancellationToken)).MessagesCount != 0, timeout);
        }

        private static async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            while (token.IsCancellationRequested == false)
            {
                if (await condition())
                    break;

                await Task.Delay(100.Milliseconds(), token);
            }
        }

        private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            while (token.IsCancellationRequested == false)
            {
                if (condition())
                    break;

                await Task.Delay(100.Milliseconds(), token);
            }
        }


        private async Task<IAsyncDisposable> QueueDeclareAsync(string queueName)
        {
            await _rabbit.QueueDeclareAsync(queueName, false, false, true, cancellationToken: _cancellationToken);
            await _rabbit.QueuePurgeAsync(queueName, _cancellationToken);

            return new AsyncDisposable(async () =>
            {
                await _rabbit.QueueDeleteAsync(queueName, cancellationToken: _cancellationToken);
                await _rabbit.QueueDeleteAsync(_options.NamingConventions.ErrorQueueName(queueName), cancellationToken: _cancellationToken);
                await _rabbit.QueueDeleteAsync(_options.NamingConventions.RetryQueueName(queueName), cancellationToken: _cancellationToken);
            });
        }

        private async Task<IAsyncDisposable> ExchangeDeclareAsync(string exchangeName)
        {
            await _rabbit.ExchangeDeclareAsync(exchangeName, "direct", false, true);

            return new AsyncDisposable(async () => await _rabbit.ExchangeDeleteAsync(new Exchange(exchangeName)));
        }

        private class AsyncDisposable : IAsyncDisposable
        {
            private readonly Func<Task> _action;

            public AsyncDisposable(Func<Task> action)
            {
                _action = action;
            }

            public async ValueTask DisposeAsync()
            {
                await _action();
            }
        }
    }
}