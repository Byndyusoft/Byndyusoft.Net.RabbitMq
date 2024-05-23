using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class RpcExample : BackgroundService
    {
        private readonly IRabbitMqClient _rabbitMqClient;

        public RpcExample(IRabbitMqClientFactory rabbitMqClientFactory)
        {
            _rabbitMqClient = rabbitMqClientFactory.CreateClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = "rpc-example";

            using var consumer = _rabbitMqClient.SubscribeRpcAsJson<RpcRequest, RpcResponse>(queueName,
                    async (request, _) =>
                    {
                        await Task.Yield();
                        checked
                        {
                            return new RpcResponse
                            {
                                Sum = request!.Number1 + request.Number2
                            };
                        }
                    })
                .WithDeclareSubscribingQueue(options => options.AsAutoDelete(true))
                .Start();

            await Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var request = new RpcRequest { Number1 = rand.Next(int.MaxValue / 5), Number2 = rand.Next() };

                    try
                    {
                        var response = await _rabbitMqClient.MakeRpcAsJson<RpcRequest, RpcResponse>(queueName, request, cancellationToken: stoppingToken);

                        Console.WriteLine($"{request.Number1}+{request.Number2}={response!.Sum}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{request.Number1}+{request.Number2}: {e.Message}");
                    }
                   
                    await Task.Delay(TimeSpan.FromMilliseconds(rand.NextDouble()*10), stoppingToken);
                }
            }, stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private class RpcRequest
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
        }

        private class RpcResponse
        {
            public int Sum { get; set; }
        }
    }
}