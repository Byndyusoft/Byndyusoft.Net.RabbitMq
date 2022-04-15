using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Core;
using Byndyusoft.Messaging.Tests.Functional.Models;
using EasyNetQ;
using EasyNetQ.Topology;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using RabbitMQ.Client.Exceptions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Byndyusoft.Messaging.Tests.Functional
{
    public class RabbitMqClientFunctionalTests : IDisposable
    {
        private readonly IBus _bus;
        private readonly IRabbitMqClient _client;
        private readonly RabbitMqClientOptions _options;
        private readonly IAdvancedBus _rabbit;

        public RabbitMqClientFunctionalTests()
        {
            _options = new RabbitMqClientOptions
            {
                ConnectionString = "host=localhost;username=guest;password=guest"
            };

            _client = new RabbitMqClient(new RabbitMqClientHandler(_options.ConnectionString), _options);
            _bus = RabbitHutch.CreateBus(_options.ConnectionString, _ => { });
            _rabbit = _bus.Advanced;
        }

        public void Dispose()
        {
            _client.Dispose();
            _bus.Dispose();
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
            var message = new RabbitMqMessage
            {
                Mandatory = true,
                Persistent = true,
                Exchange = null,
                RoutingKey = queueName,
                Content = JsonContent.Create(data, options: serializationOptions),
                Properties = new RabbitMqMessageProperties
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
                },
                Headers = new RabbitMqMessageHeaders
                {
                    {"key", "value"}
                }
            };
            await _client.PublishMessageAsync(message);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // assert
            using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync();

            pullingResult.IsAvailable.Should().BeTrue();
            JsonSerializer.Deserialize<Message>(pullingResult.Body).Should().BeEquivalentTo(data);
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
            var message = new RabbitMqMessage
            {
                Mandatory = true,
                Persistent = true,
                Exchange = exchangeName,
                RoutingKey = routingKey,
                Content = JsonContent.Create(data, options: serializationOptions),
                Properties = new RabbitMqMessageProperties
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
                },
                Headers = new RabbitMqMessageHeaders
                {
                    {"key", "value"}
                }
            };
            await _client.PublishMessageAsync(message);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // assert
            using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync();

            pullingResult.IsAvailable.Should().BeTrue();

            pullingResult.ReceivedInfo.Queue.Should().Be(queueName);
            pullingResult.ReceivedInfo.Exchange.Should().Be(exchangeName);
            pullingResult.ReceivedInfo.RoutingKey.Should().Be(routingKey);
            JsonSerializer.Deserialize<Message>(pullingResult.Body).Should().BeEquivalentTo(data);
            pullingResult.Properties.Headers["key"].Should().BeOfType<byte[]>().Subject.Should()
                .BeEquivalentTo(Encoding.UTF8.GetBytes("value"));
            pullingResult.Properties.ContentType.Should().Be(message.Properties.ContentType);
            pullingResult.Properties.Priority.Should().Be(message.Properties.Priority);
            pullingResult.Properties.Type.Should().Be(message.Properties.Type);
            pullingResult.Properties.ContentEncoding.Should().Be(message.Properties.ContentEncoding);
            pullingResult.Properties.CorrelationId.Should().Be(message.Properties.CorrelationId);
            pullingResult.Properties.Expiration.Should()
                .Be(message.Properties.Expiration?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
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
            using var message = await _client.GetMessageAsync(queueName);

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
                Expiration = "10000",
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
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, properties, body);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            using var message = await _client.GetMessageAsync(queueName);

            message.Should().NotBeNull();
            message!.Content.ReadFromJsonAsync<Message>().GetAwaiter().GetResult().Should().BeEquivalentTo(data);
            message.Headers["key"].Should().Be("value");

            CheckProperties(message.Properties, properties);
        }

        [Fact]
        public async Task CompleteMessage_Ack_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_Ack_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, new MessageProperties(),
                Array.Empty<byte>());
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            var message = await _client.GetMessageAsync(queueName);

            // act
            await _client.CompleteMessageAsync(message!, ClientConsumeResult.Ack);

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(new Queue(queueName));
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CompleteMessage_RejectWithoutRequeue_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_RejectWithoutRequeue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, new MessageProperties(),
                Array.Empty<byte>());
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            var message = await _client.GetMessageAsync(queueName);

            // act
            await _client.CompleteMessageAsync(message!, ClientConsumeResult.RejectWithoutRequeue);

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(new Queue(queueName));
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CompleteMessage_RejectWithRequeue_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_RejectWithRequeue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            var body = Array.Empty<byte>();
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, properties, body);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            var message = await _client.GetMessageAsync(queueName);

            // act
            await _client.CompleteMessageAsync(message!, ClientConsumeResult.RejectWithRequeue);

            // assert
            using var consumer = _rabbit.CreatePullingConsumer(new Queue(queueName));
            var pullingResult = await consumer.PullAsync();

            pullingResult.IsAvailable.Should().BeTrue();
            pullingResult.Body.Should().BeEquivalentTo(body);
            pullingResult.Properties.MessageId.Should().Be(properties.MessageId);
        }

        [Fact]
        public async Task CompleteMessage_Error_Test()
        {
            // arrange
            var queueName = $"{nameof(CompleteMessage_Error_Test)}.queue";
            var errorQueueName = _options.NamingConventions.ErrorQueueName(queueName);
            await using var queue = await QueueDeclareAsync(queueName);

            var body = Array.Empty<byte>();
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, properties, body);
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            var message = await _client.GetMessageAsync(queueName);

            // act
            await _client.CompleteMessageAsync(message!, ClientConsumeResult.Error);
            await WaitForMessageAsync(errorQueueName, TimeSpan.FromSeconds(5));

            // assert
            using var consumer = _rabbit.CreatePullingConsumer(new Queue(errorQueueName));
            var pullingResult = await consumer.PullAsync();

            pullingResult.IsAvailable.Should().BeTrue();
            pullingResult.Body.Should().BeEquivalentTo(body);
            pullingResult.Properties.MessageId.Should().Be(properties.MessageId);

            _rabbit.GetQueueStats(new Queue(queueName)).MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(CreateQueue_Test)}.queue";

            // act
            await _client.CreateQueueAsync(queueName, QueueOptions.Default);

            // assert
            using var scope = new AssertionScope();
            QueueExists(queueName).Should().BeTrue();

            // cleanup
            await _rabbit.QueueDeleteAsync(new Queue(queueName));
        }

        [Fact]
        public async Task QueueExists_True_Test()
        {
            // arrange
            var queueName = $"{nameof(QueueExists_True_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);

            // act
            var result = await _client.QueueExistsAsync(queueName);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task QueueExists_False_Test()
        {
            // arrange
            var queueName = $"{nameof(QueueExists_False_Test)}.queue";

            // act
            var result = await _client.QueueExistsAsync(queueName);

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
            await _client.DeleteQueueAsync(queueName);

            // assert
            QueueExists(queueName).Should().BeFalse();
        }

        [Fact]
        public async Task PurgeQueue_Test()
        {
            // arrange
            var queueName = $"{nameof(PurgeQueue_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, new MessageProperties(),
                Array.Empty<byte>());
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            await _client.PurgeQueueAsync(queueName);

            // assert
            var stats = await _rabbit.GetQueueStatsAsync(new Queue(queueName));
            stats.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task GetQueueMessageCount_Test()
        {
            // arrange
            var queueName = $"{nameof(GetQueueMessageCount_Test)}.queue";
            await using var queue = await QueueDeclareAsync(queueName);
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, new MessageProperties(),
                Array.Empty<byte>());
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            // act
            var messageCount = await _client.GetQueueMessageCountAsync(queueName);

            // assert
            messageCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateExchange_Test()
        {
            // arrange
            var exchangeName = $"{nameof(CreateExchange_Test)}.exchange";

            // act
            await _client.CreateExchangeAsync(exchangeName, ExchangeOptions.Default);

            // assert
            using var scope = new AssertionScope();
            ExchangeExists(exchangeName).Should().BeTrue();

            // cleanup
            await _rabbit.ExchangeDeleteAsync(new Exchange(exchangeName));
        }

        [Fact]
        public async Task ExchangeExists_True_Test()
        {
            // arrange
            var exchangeName = $"{nameof(ExchangeExists_True_Test)}.exchange";
            await using var exchange = await ExchangeDeclareAsync(exchangeName);

            // act
            var result = await _client.ExchangeExistsAsync(exchangeName);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExchangeExists_False_Test()
        {
            // arrange
            var exchangeName = $"{nameof(ExchangeExists_False_Test)}.exchange";

            // act
            var result = await _client.ExchangeExistsAsync(exchangeName);

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
            await _client.DeleteExchangeAsync(exchangeName);

            // assert
            ExchangeExists(exchangeName).Should().BeFalse();
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
            await _client.BindQueueAsync(exchangeName, routingKey, queueName);

            // assert
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, new MessageProperties(),
                Array.Empty<byte>());
            await WaitForMessageAsync(queueName, TimeSpan.FromSeconds(5));

            _rabbit.GetQueueStats(new Queue(queueName)).MessagesCount.Should().Be(1);
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
                return Task.CompletedTask;
            }).Start();

            // act
            var properties = new MessageProperties {MessageId = "id"};
            await _rabbit.PublishAsync(Exchange.GetDefault(), queueName, true, properties, Array.Empty<byte>());

            // assert
            await WaitForAsync(() => receivedMessage is not null, TimeSpan.FromSeconds(5));
            receivedMessage.Should().NotBeNull();
            receivedMessage!.Properties.MessageId.Should().Be(properties.MessageId);
        }

        private bool QueueExists(string queueName)
        {
            try
            {
                _rabbit.QueueDeclarePassive(queueName);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        private bool ExchangeExists(string exchangeName)
        {
            try
            {
                _rabbit.ExchangeDeclarePassive(exchangeName);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
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
            actual.Expiration.Should().Be(int.Parse(expected.Expiration).Milliseconds());
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
            actual.Expiration.Should()
                .Be(expected.Expiration?.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            actual.AppId.Should().Be(expected.AppId);
            actual.MessageId.Should().Be(expected.MessageId);
            actual.ReplyTo.Should().Be(expected.ReplyTo);
            actual.Timestamp.Should().Be(new DateTimeOffset(expected.Timestamp!.Value).ToUnixTimeMilliseconds());
            actual.UserId.Should().Be(expected.UserId);
        }

        private Task WaitForMessageAsync(string queueName, TimeSpan timeout)
        {
            return WaitForAsync(() => _rabbit.GetQueueStats(new Queue(queueName)).MessagesCount != 0, timeout);
        }

        private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
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
            await _rabbit.QueueDeclareAsync(queueName, false, false, true);
            await _rabbit.QueuePurgeAsync(new Queue(queueName));

            return new AsyncDisposable(async () =>
            {
                await _rabbit.QueueDeleteAsync(new Queue(queueName));
                await _rabbit.QueueDeleteAsync(new Queue(_options.NamingConventions.ErrorQueueName(queueName)));
                await _rabbit.QueueDeleteAsync(new Queue(_options.NamingConventions.RetryQueueName(queueName)));
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