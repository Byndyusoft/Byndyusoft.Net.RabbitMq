using System;
using System.Net.Http;
using System.Net.Http.MessagePack;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Utils;
using MessagePack;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public static class RabbitMqClientMessagePackExtensions
    {
        public static IRabbitMqConsumer SubscribeAsMessagePack<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage,
            MessagePackSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.SubscribeAs(queueName, onMessage, ModelDeserializer);

            Task<T?> ModelDeserializer(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                return message.Content.ReadFromMessagePackAsync<T>(options, token)!;
            }
        }

        public static Task PublishAsMessagePackAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            MessagePackSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            return client.PublishAsync(exchangeName, routingKey, model, ModelSerializer, cancellationToken);

            HttpContent ModelSerializer(T arg)
            {
                return MessagePackContent.Create(model, options);
            }
        }
    }


    public class SubscribeAsMessagePackExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public SubscribeAsMessagePackExample(IRabbitMqClientFactory rabbitMqClientFactory)
        {
            _rabbitMqClient = rabbitMqClientFactory.CreateClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string queueName = "messagepack-example";

            using var consumer = _rabbitMqClient.SubscribeAsMessagePack<Message>(queueName,
                    (model, _) =>
                    {
                        Console.WriteLine(JsonSerializer.Serialize(model));
                        return Task.FromResult(ConsumeResult.Ack);
                    })
                .WithPrefetchCount(20)
                .WithDeclareSubscribingQueue(options => options.AsAutoDelete(true))
                .Start();

            await Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var message = new Message { Property = "messagepack-example" };
                    await _rabbitMqClient.PublishAsMessagePackAsync(null, queueName, message, null, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(rand.NextDouble()), stoppingToken);
                }
            }, stoppingToken);
        }
    }
}