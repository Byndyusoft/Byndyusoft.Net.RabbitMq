using System;
using System.Net.Http.MessagePack;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Utils;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

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

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromMessagePackAsync<T>(options, token)
                    .ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }

            return client.Subscribe(queueName, OnMessage);
        }

        public static async Task PublishAsMessagePackAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            MessagePackSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            var message = new RabbitMqMessage
            {
                Content = MessagePackContent.Create(model, options),
                Exchange = exchangeName,
                RoutingKey = routingKey,
                Persistent = true,
                Mandatory = true
            };
            await client.PublishMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }


    public class SubscribeAsMessagePackExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public SubscribeAsMessagePackExample(IRabbitMqClient rabbitMqClient)
        {
            _rabbitMqClient = rabbitMqClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "messagepack-example";

            using var consumer = _rabbitMqClient.SubscribeAsMessagePack<Message>(queueName,
                    (model, _) =>
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(model));
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