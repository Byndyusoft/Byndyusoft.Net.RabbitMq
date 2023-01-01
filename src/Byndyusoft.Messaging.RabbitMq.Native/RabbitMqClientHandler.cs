using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Native.Internal;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Byndyusoft.Messaging.RabbitMq.Native
{
    public class RabbitMqClientHandler : Disposable, IRabbitMqClientHandler
    {
        private ConnectionFactory? _connectionFactory;
        private IConnection? _connection;
        private RabbitMqEndpoint? _endPoint;
        private readonly ILogger<RabbitMqClientHandler> _logger;
        private SemaphoreSlim? _mutex = new(1, 1);

        public RabbitMqClientHandler(
            IOptions<RabbitMqClientOptions> options,
            ILogger<RabbitMqClientHandler>? logger = null)
        {
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotNull(options.Value.ConnectionString, nameof(RabbitMqClientOptions.ConnectionString));

            _logger = logger ?? new NullLogger<RabbitMqClientHandler>();
            Options = options.Value;
        }

        internal RabbitMqClientOptions Options { get; }

        RabbitMqEndpoint IRabbitMqEndpointContainer.Endpoint
        {
            get
            {
                Preconditions.CheckNotDisposed(this);

                return _endPoint ??=
                    new RabbitMqEndpoint
                    {
                        Name = ConnectionFactory.Endpoint.HostName,
                        Port = (ushort)ConnectionFactory.Endpoint.Port
                    };
            }
        }

        public async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);

            var getResult = model.BasicGet(queueName, false);
            var message = ReceivedRabbitMqMessageFactory.CreatePulledMessage(queueName, getResult);
            return message;
        }

        public async Task<ulong> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            var count = model.MessageCount(queueName);
            return count;
        }

        public async Task AckMessageAsync(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.BasicAck(message.DeliveryTag, false);
        }

        public async Task RejectMessageAsync(ReceivedRabbitMqMessage message, bool requeue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.BasicNack(message.DeliveryTag, false, requeue);
        }

        public async Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);

            model.BasicReturn += OnBasicReturn;

            var (body, properties) = await RabbitMqMessageFactory.CreateMessage(model, message);
            
            model.BasicPublish(
                message.Exchange ?? string.Empty,
                message.RoutingKey,
                message.Mandatory,
                properties,
                body);
        }

        private async void OnBasicReturn(object sender, BasicReturnEventArgs e)
        {
            try
            {
                var returnMessage = ReceivedRabbitMqMessageFactory.CreateReturnedMessage(e);
                var task = MessageReturned?.Invoke(returnMessage, CancellationToken.None);
                if (task is not null)
                    await task.Value;
            }
            catch 
            {
               // do nothing
            }
        }

        public async Task<IDisposable> StartConsumeAsync(
            string queueName, 
            bool? exclusive, 
            ushort? prefetchCount, 
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> onMessage,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            try
            {
                if (prefetchCount is not null)
                {
                    model.BasicQos(0, prefetchCount.Value, false);
                }

                return new RabbitMqAsyncConsumer(model, queueName, exclusive, onMessage);
            }
            catch
            {
                model.Dispose();
                throw;
            }
        }

        public event ReturnedRabbitMqMessageHandler? MessageReturned;

        public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.QueuePurge(queueName);
        }

        public async Task CreateQueueAsync(string queueName, QueueOptions options, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.QueueDeclare(queueName, options.Durable, options.Exclusive, options.AutoDelete);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                model.QueueDeclarePassive(queueName);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public async Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.QueueDelete(queueName, ifUnused);
        }

        public async Task CreateExchangeAsync(string exchangeName, ExchangeOptions options, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);

            model.ExchangeDeclare(exchangeName, options.Type.ToString().ToLower(), options.Durable, options.AutoDelete);
        }

        public async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                model.ExchangeDeclarePassive(exchangeName);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.ExchangeDelete(exchangeName, ifUnused);
        }

        public async Task BindQueueAsync(string exchangeName, string routingKey, string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            using var model = await CreateModelAsync(cancellationToken)
                .ConfigureAwait(false);
            model.QueueBind(queueName, exchangeName, routingKey);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            _connection?.Dispose();
            _connection = null;

            _mutex?.Dispose();
            _mutex = null;
        }

        private async Task<IConnection> ConnectIfNeededAsync(CancellationToken cancellationToken = default)
        {
            await _mutex!.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _mutex.Release();
            }

            return _connection!;
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            while (_connection?.IsOpen != true && cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    _connection = ConnectionFactory.CreateConnection();
                    break;
                }
                catch (BrokerUnreachableException exception)
                {
                    var interval = ConnectionFactory.NetworkRecoveryInterval;
                    var message = $"{exception.Message}. Wait {interval} and try connect again";
                    _logger.LogError(message);

                    await Task.Delay(interval, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task<IModel> CreateModelAsync(CancellationToken cancellationToken = default)
        {
            var connection = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            var model = connection.CreateModel();

            if (Options.ConnectionStringBuilder.PublisherConfirms)
            {
                model.ConfirmSelect();
            }

            var prefetchCount = Options.ConnectionStringBuilder.PrefetchCount;
            model.BasicQos(0, prefetchCount, false);
            
            return model;
        }

        private ConnectionFactory ConnectionFactory => _connectionFactory ??= CreateConnectionFactory();

        private ConnectionFactory CreateConnectionFactory()
        {
            var connectionString = Options.ConnectionStringBuilder;
            var host = connectionString.Hosts.SingleOrDefault();
            if (host is null)
                throw new ArgumentException("At least one host should be provided");
            
            return new ConnectionFactory
            {
                Port = host.Port,
                HostName = host.Name,
                VirtualHost = connectionString.VirtualHost,
                UserName = connectionString.UserName,
                Password = connectionString.Password,
                ContinuationTimeout = connectionString.Timeout,
                RequestedConnectionTimeout = connectionString.RequestedConnectionTimeout,
                RequestedHeartbeat = connectionString.RequestedHeartbeat,
                AutomaticRecoveryEnabled = true
            };
        }
    }
}
