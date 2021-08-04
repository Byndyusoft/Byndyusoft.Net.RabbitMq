using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using EasyNetQ.Topology;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IQueueService" />
    public sealed class QueueService : IQueueService
    {
        private readonly IBusFactory _busFactory;
        private readonly RabbitMqConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     RabbitMq connection
        /// </summary>
        private IBus _bus;

        /// <summary>
        ///     True, if service has been already initialized
        /// </summary>
        private bool _isInitialized;

        public QueueService(IBusFactory busFactory, RabbitMqConfiguration configuration, IServiceProvider serviceProvider)
        {
            _busFactory = busFactory ?? throw new ArgumentNullException(nameof(busFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public async Task Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            ValidateConfig(_configuration);

            _bus = _busFactory.CreateBus(_configuration.ConnectionString);

            _bus.Advanced.MessageReturned += OnMessageReturned;


            //_exchange = await _bus.Advanced.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct).ConfigureAwait(false);

            //if (string.IsNullOrEmpty(_settings.ErrorRoutingKey) == false)
            //    await InitializeQueue(_bus, _exchange, new QueueSettings { RoutingKey = _settings.ErrorRoutingKey }).ConfigureAwait(false);

            //if (string.IsNullOrEmpty(_settings.IncomingQueue) == false)
            //    await _bus.Advanced.QueueDeclareAsync(_settings.IncomingQueue).ConfigureAwait(false);

            //foreach (var routingKey in _incomingRoutingKeysDictionary.Values)
            //    if (string.IsNullOrEmpty(routingKey) == false)
            //        await InitializeQueue(_bus, _exchange, _queueSettingsDictionary[routingKey]).ConfigureAwait(false);

            //foreach (var routingKey in _outgoingRoutingKeysDictionary.Values)
            //    if (string.IsNullOrEmpty(routingKey) == false)
            //        await InitializeQueue(_bus, _exchange, _queueSettingsDictionary[routingKey]).ConfigureAwait(false);

            _isInitialized = true;
        }

        /// <summary>
        ///     Validates topology, throws exception on incorrect settings
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateConfig(RabbitMqConfiguration configuration)
        {
            var errors = new StringBuilder();
            if (configuration.ExchangeConfigurations == null ||
                configuration.ExchangeConfigurations.Any() == false)
            {
                errors.AppendLine("Empty exchages configuration");
            }
            else
            {
                foreach (var exchangeCfg in configuration.ExchangeConfigurations)
                {
                    ValidateConfig(exchangeCfg.Key, exchangeCfg.Value, errors);
                }
            }

            var error = errors.ToString();
            if (string.IsNullOrEmpty(error) == false)
                throw new InvalidOperationException(error);
        }

        /// <summary> 
        ///     Validates exchange configuration
        /// </summary>
        private void ValidateConfig(
            string exchangeName,
            ExchangeConfiguration exchangeCfg,
            StringBuilder errors)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                errors.AppendLine("Empty exchange key");

            if (exchangeCfg == null)
            {
                errors.AppendLine("Empty exchange configuration");
                return;
            }

            if (string.IsNullOrWhiteSpace(exchangeCfg.ExchangeName))
                errors.AppendLine("Empty exchange name");

            if (exchangeCfg.ExchangeName != exchangeName)
            {
                errors.AppendLine($"Exchange key {exchangeName} doesn't match exchange name {exchangeCfg.ExchangeName}");
            }

            if (ContainsUnsupportedCharacters(exchangeCfg.ExchangeName))
            {
                errors.AppendLine($"Exchange name {exchangeCfg.ExchangeName} contains unsupported symbols");
            }

            if ((exchangeCfg.ConsumeQueueConfigurations == null ||
                 exchangeCfg.ConsumeQueueConfigurations.Any() == false) &&
                (exchangeCfg.ProduceQueueConfigurations == null ||
                 exchangeCfg.ProduceQueueConfigurations.Any() == false))
            {
                errors.AppendLine("Exchange contains neither consuming pipelines, not producing pipelines");
            }

            foreach (var queue in exchangeCfg.ConsumeQueueConfigurations)
            {
                ValidateConfig(queue, errors);
            }

            foreach (var queue in exchangeCfg.ProduceQueueConfigurations)
            {
                ValidateConfig(queue, errors);
            }
        }

        /// <summary>
        ///     Validates queue configuration
        /// </summary>
        private void ValidateConfig(QueueConfiguration queueCfg, StringBuilder errors)
        {
            if (queueCfg == null)
            {
                errors.AppendLine("Empty queue configuration");
                return;
            }

            if (string.IsNullOrWhiteSpace(queueCfg.QueueName))
                errors.AppendLine("Empty queue name");

            if (string.IsNullOrWhiteSpace(queueCfg.RoutingKey))
                errors.AppendLine("Empty queue routing key");

            if (ContainsUnsupportedCharacters(queueCfg.QueueName))
            {
                errors.AppendLine($"Queue name {queueCfg.QueueName} contains unsupported symbols");
            }

            if (ContainsUnsupportedCharacters(queueCfg.RoutingKey))
            {
                errors.AppendLine($"Queue routing key {queueCfg.RoutingKey} contains unsupported symbols");
            }
        }

        private static bool ContainsUnsupportedCharacters(string name)
        {
            return name.ToCharArray().Where(x => x != '_' && x != '.').All(char.IsLetterOrDigit) == false;
        }

        private void OnMessageReturned(object sender, MessageReturnedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task Publish<TMessage>(TMessage message, Dictionary<string, string>? headers = null, Action<MessageReturnedEventArgs>? returnedHandled = null) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage)
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages()
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages(string routingKey)
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_bus != null)
            {
                _bus.Advanced.MessageReturned -= OnMessageReturned;
                _bus.Dispose();
            }

            _isInitialized = false;
        }
    }
}