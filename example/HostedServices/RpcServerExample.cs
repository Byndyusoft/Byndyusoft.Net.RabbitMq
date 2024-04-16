using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq;
using Byndyusoft.Messaging.RabbitMq.Rpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Byndyusoft.Net.RabbitMq.HostedServices
{
    public class MathRpcService : IRpcService
    {
        [RpcMethod(MathRpcServiceClient.SumQueueName, PrefetchCount = 10)]
        public Task<RpcResponse> Sum(
            [RpcRequest] RpcRequest request,
            [RpcFromServices] ILogger<MathRpcService> logger,
            CancellationToken cancellationToken)
        {
            checked
            {
                logger.LogDebug($"Server: {request.Number1}+{request.Number2}");

                return Task.FromResult(new RpcResponse
                {
                    Sum = request.Number1 + request.Number2
                });
            }
        }
    }

    public class MathRpcServiceClient
    {
        public const string SumQueueName = "rpc-service-example";

        private readonly IRabbitMqClient _rabbitMqClient;

        public MathRpcServiceClient(IRabbitMqClientFactory rabbitMqClientFactory)
        {
            _rabbitMqClient = rabbitMqClientFactory.CreateClient();
        }

        public async Task<int> Sum(int x, int y, CancellationToken cancellationToken)
        {
            var request = new RpcRequest
            {
                Number1 = x,
                Number2 = y
            };
            var response = await _rabbitMqClient.MakeRpcAsJson<RpcRequest, RpcResponse>(
                SumQueueName, request, cancellationToken: cancellationToken);
            return response!.Sum;
        }
    }

    public class RpcServerExample : BackgroundService
    {
        private readonly MathRpcServiceClient _mathRpcServiceClient;

        public RpcServerExample(MathRpcServiceClient mathRpcServiceClient)
        {
            _mathRpcServiceClient = mathRpcServiceClient;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                var rand = new Random();
                while (stoppingToken.IsCancellationRequested == false)
                {
                    var x = rand.Next(int.MaxValue / 2);
                    var y = rand.Next();

                    try
                    {
                        var sum = await _mathRpcServiceClient.Sum(x, y, stoppingToken);
                        Console.WriteLine($"{x}+{y}={sum}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{x}+{y}: {e.Message}");
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(rand.NextDouble() * 10), stoppingToken);
                }
            }, stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    public class RpcRequest
    {
        public int Number1 { get; set; }
        public int Number2 { get; set; }
    }

    public class RpcResponse
    {
        public int Sum { get; set; }
    }
}