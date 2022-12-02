using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Rpc.Internal;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq.Rpc
{
    internal class RpcHostedService : IHostedService
    {
        private readonly IRabbitMqClient _rabbitMqClient;
        private readonly RabbitMqRpcOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IRabbitMqConsumer> _consumers = new();
        private readonly IEnumerable<IRpcService> _rcpServices;
        private readonly ILogger<RpcHostedService> _logger;

        public RpcHostedService(
            IRabbitMqClient rabbitMqClient,
            IOptions<RabbitMqRpcOptions> options,
            IEnumerable<IRpcService> rcpServices,
            IServiceProvider serviceProvider,
            ILogger<RpcHostedService> logger)
        {
            _rabbitMqClient = rabbitMqClient;
            _options = options.Value;
            _rcpServices = rcpServices;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            foreach (var rpcService in _rcpServices)
            {
                var rpcMethods = rpcService.GetType().GetRuntimeMethods().ToArray();
                foreach (var rpcMethod in rpcMethods)
                {
                    var rpcQueue = rpcMethod.GetCustomAttribute<RpcQueueAttribute>();
                    if (rpcQueue is null)
                        continue;

                    var queueName =
                        _options.QueueNames.GetValueOrDefault(rpcQueue.QueueName) ?? rpcQueue.QueueName;
                    var queueOptions =
                        _options.QueueOption(queueName) ?? QueueOptions.Default;

                    var consumer = _rabbitMqClient
                        .SubscribeRpc(queueName,
                            (message, cancellationToken) =>
                                OnRpcCall(rpcService, rpcMethod, message, cancellationToken))
                        .WithDeclareSubscribingQueue(queueOptions);
                    if (rpcQueue.PrefetchCount != 0)
                    {
                        consumer = consumer.WithPrefetchCount(rpcQueue.PrefetchCount);
                    }

                    await consumer.StartAsync(stoppingToken)
                        .ConfigureAwait(false);
                    _consumers.Add(consumer);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _consumers.ForEach(consumer => consumer.Dispose());
            _consumers.Clear();

            return Task.CompletedTask;
        }

        private async Task<RpcResult> OnRpcCall(
            object rpcService,
            MethodInfo rpcMethod,
            ReceivedRabbitMqMessage message,
            CancellationToken cancellationToken)
        {
            var taskType = rpcMethod.ReturnType;
            var isTask = taskType.GetGenericTypeDefinition() == typeof(Task<>) ||
                         taskType.GetGenericTypeDefinition() == typeof(ValueTask<>);
            if (isTask is false)
                throw new InvalidOperationException("Only Task<T> or ValueTask<T> return type supported");

            var parameters = new object?[rpcMethod.GetParameters().Length];
            for (int i = 0; i < rpcMethod.GetParameters().Length; i++)
            {
                var paramInfo = rpcMethod.GetParameters()[i];

                if (paramInfo.GetCustomAttribute<RpcRequestAttribute>() is not null)
                {
                    parameters[i] = await message.Content.ReadAsAsync(paramInfo.ParameterType, cancellationToken);
                    continue;
                }

                if (paramInfo.GetCustomAttribute<RpcFromServicesAttribute>() is not null)
                {
                    parameters[i] = _serviceProvider.GetRequiredService(paramInfo.ParameterType);
                    continue;
                }

                if (paramInfo.ParameterType == typeof(CancellationToken))
                {
                    parameters[i] = cancellationToken;
                    continue;
                }

                throw new ArgumentException("Unsupported parameter", paramInfo.Name);
            }

            try
            {
                var response = await rpcMethod.InvokeAsync(rpcService, parameters)
                    .ConfigureAwait(false);
                var returnType = taskType.GetGenericArguments()[0];
                var writer =
                    MediaTypeFormatterCollection.Default
                        .FindWriter(returnType, message.Content.Headers.ContentType)
                    ?? throw new UnsupportedMediaTypeException(string.Empty, message.Content.Headers.ContentType);

                var content = new ObjectContent(returnType, response, writer);
                return RpcResult.Success(content);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                return RpcResult.Error(exception);
            }
        }
    }
}