//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Byndyusoft.Net.RabbitMq.Abstractions;
//using Byndyusoft.Net.RabbitMq.Models;
//using Byndyusoft.Net.RabbitMq.Services;
//using EasyNetQ;
//using EasyNetQ.SystemMessages;
//using EasyNetQ.Topology;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using OpenTracing;
//using OpenTracing.Propagation;
//using OpenTracing.Tag;

//namespace Byndyusoft.Net.RabbitMq.Tests
//{
//    using JsonSerializer = EasyNetQ.JsonSerializer;

//    public class RabbitQueueService
//    {
//        private const string MessageKeyHeader = "MessageKey";
//        private readonly ILogger<RabbitQueueService> _logger;
//        private readonly RabbitSettings _settings;
//        private readonly ITracer _tracer;
//        private readonly IBusFactory _busService;
//        private IBus? _bus;
//        private IExchange? _exchange;
//        private bool _isInitialized;
//        private readonly Dictionary<Type, string> _incomingRoutingKeysDictionary;
//        private readonly Dictionary<Type, string> _outgoingRoutingKeysDictionary;
//        private readonly Dictionary<string, QueueSettings> _queueSettingsDictionary;

//        public RabbitQueueService(ILogger<RabbitQueueService> logger,
//                                  IOptions<RabbitSettings> options,
//                                  ITracer tracer,
//                                  IBusFactory busService)
//        {
//            _logger = logger;
//            _tracer = tracer;
//            _busService = busService;
//            _settings = options.Value;
//            _incomingRoutingKeysDictionary = new Dictionary<Type, string>();
//            _outgoingRoutingKeysDictionary = new Dictionary<Type, string>();
//            _queueSettingsDictionary = new Dictionary<string, QueueSettings>();
//        }

//        public event Func<MessageReturnedEventArgs, Task>? MessageReturned;

//        public async Task Initialize()
//        {
//            if (_isInitialized)
//            {
//                _logger.LogError("Already initialized");
//                return;
//            }

//            ValidateSettings(_settings);

//            _bus = _busService.CreateBus(_settings.ConnectionString);

//            _bus.Advanced.MessageReturned += OnMessageReturned;

//            var conventions = _bus.Advanced.Container.Resolve<IConventions>();

//            //_bus.Advanced.Conventions.ErrorQueueNamingConvention = info => $"{info.Exchange}.{info.RoutingKey}.error";
//            //_bus.Advanced.Conventions.ErrorExchangeNamingConvention = info => $"{info.Exchange}.error";

//            _exchange = await _bus.Advanced.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct).ConfigureAwait(false);

//            if (string.IsNullOrEmpty(_settings.ErrorRoutingKey) == false)
//                await InitializeQueue(_bus, _exchange, new QueueSettings { RoutingKey = _settings.ErrorRoutingKey }).ConfigureAwait(false);

//            if (string.IsNullOrEmpty(_settings.IncomingQueue) == false)
//                await _bus.Advanced.QueueDeclareAsync(_settings.IncomingQueue).ConfigureAwait(false);

//            foreach (var routingKey in _incomingRoutingKeysDictionary.Values)
//                if (string.IsNullOrEmpty(routingKey) == false)
//                    await InitializeQueue(_bus, _exchange, _queueSettingsDictionary[routingKey]).ConfigureAwait(false);

//            foreach (var routingKey in _outgoingRoutingKeysDictionary.Values)
//                if (string.IsNullOrEmpty(routingKey) == false)
//                    await InitializeQueue(_bus, _exchange, _queueSettingsDictionary[routingKey]).ConfigureAwait(false);

//            _isInitialized = true;
//        }

//        public async Task ResendErrorMessages()
//        {
//            if (_isInitialized == false)
//                throw new InvalidOperationException("Initialize bus before use");

//            foreach (var incomingQueue in _incomingRoutingKeysDictionary)
//                await ResendErrorMessages(incomingQueue.Value).ConfigureAwait(false);
//        }

//        public async Task ResendErrorMessages(string incomingRoutingKey)
//        {
//            var queue = await _bus.Advanced
//                                  .QueueDeclareAsync($"{_exchange.Name}.{incomingRoutingKey}.error")
//                                  .ConfigureAwait(false);

//            var messageCount = _bus.Advanced.MessageCount(queue);
//            var jsonSerializer = new JsonSerializer();

//            while (messageCount > 0)
//            {
//                var getResult = _bus.Advanced.Get<Error>(queue);

//                if (getResult.MessageAvailable)
//                {
//                    var error = getResult.Message.Body;
//                    var headers = error.BasicProperties.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());

//                    var properties = GetMessageProperties();

//                    foreach (var header in headers)
//                        properties.Headers.Add(header.Key, header.Value);

//                    //TODO: избавиться от рефлексии
//                    var messageBodyType = Type.GetType(error.BasicProperties.Type);
//                    var messageGenericType = typeof(Message<>).MakeGenericType(messageBodyType);
//                    var package = jsonSerializer.BytesToMessage(messageBodyType, Encoding.UTF8.GetBytes(error.Message));

//                    var newMessage = Activator.CreateInstance(messageGenericType, package, properties) as EasyNetQ.IMessage;

//                    //TODO включить паблиш конфермс
//                    await _bus.Advanced
//                              .PublishAsync(_exchange, error.RoutingKey, true, newMessage)
//                              .ConfigureAwait(false);
//                }
//                else
//                {
//                    _logger.LogInformation("Message unavailable");
//                }

//                messageCount--;
//            }
//        }

//        public async Task PushDocument<TMessage>(TMessage message, string key, Dictionary<string, string>? headers = null)
//        {
//            if (_isInitialized == false)
//                throw new InvalidOperationException("Initialize bus before use");

//            if (_tracer.ActiveSpan == null)
//                throw new InvalidOperationException("No active tracing span. Push to queue will broken service chain");

//            var span = _tracer.BuildSpan(nameof(PushDocument)).Start();

//            span.SetTag(nameof(message), JsonConvert.SerializeObject(message));

//            var properties = GetMessageProperties();

//            var carrier = new HttpHeadersCarrier(properties.Headers);

//            _tracer.InjectServices(_tracer.ActiveSpan.Context, BuiltinFormats.HttpHeaders, carrier);

//            properties.Headers.Add(MessageKeyHeader, key);

//            if (headers != null)
//                foreach (var header in headers)
//                    properties.Headers.Add(header.Key, header.Value);

//            if (_outgoingRoutingKeysDictionary.TryGetValue(typeof(TMessage), out var routingKey) == false)
//                throw new InvalidOperationException($"Type {typeof(TMessage)} didnt registered for sending");

//            //TODO включить паблиш конфермс
//            await _bus.Advanced
//                      .PublishAsync(_exchange, routingKey, true, new Message<TMessage>(message, properties))
//                      .ConfigureAwait(false);

//            span.Finish();
//        }

//        //TODO: Возможно, должно быть не здесь
//        public async Task PushError<TError>(TError errorMessage, string key, Dictionary<string, string>? headers = null)
//        {
//            if (_isInitialized == false)
//                throw new InvalidOperationException("Initialize bus before use");

//            var span = _tracer.BuildSpan(nameof(PushError)).Start();

//            var properties = GetMessageProperties();

//            var carrier = new HttpHeadersCarrier(properties.Headers);

//            _tracer.InjectServices(_tracer.ActiveSpan.Context, BuiltinFormats.HttpHeaders, carrier);

//            properties.Headers.Add(MessageKeyHeader, key);

//            if (headers != null)
//                foreach (var header in headers)
//                    properties.Headers.Add(header.Key, header.Value);

//            //TODO включить паблиш конфермс
//            await _bus.Advanced
//                      .PublishAsync(_exchange, _settings.ErrorRoutingKey, true, new Message<TError>(errorMessage, properties))
//                      .ConfigureAwait(false);

//            span.Finish();
//        }

//        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage) where TMessage : class
//        {
//            var incomingQueue = _settings.IncomingQueue;

//            if (_incomingRoutingKeysDictionary.TryGetValue(typeof(TMessage), out var routingKey) == false)
//            {
//                _logger.LogError($@"Type {typeof(TMessage)} didnt registered for sending");

//                if (string.IsNullOrEmpty(incomingQueue))
//                    throw new NullReferenceException(nameof(_settings.IncomingQueue));
//            }


//            _bus.Advanced.Consume<TMessage>(new Queue(routingKey != null ? $"{_settings.ExchangeName}.{routingKey}" : incomingQueue, false), (message, messageInfo) => OnMessage(message, processMessage));
//        }

//        /// <summary>
//        ///     Обработка вернувшихся сообщений
//        /// </summary>
//        /// <param name="sender"></param>
//        /// <param name="args"></param>
//        private void OnMessageReturned(object sender, EasyNetQ.MessageReturnedEventArgs args)
//        {
//            var stringDictionary = args.MessageProperties.Headers.Where(x => x.Value.GetType() == typeof(byte[])).ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[])x.Value));
//            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
//            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

//            using (_tracer.BuildSpan(nameof(OnMessageReturned)).AddReference(References.ChildOf, spanContext).StartActive(true))
//            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId), _tracer.ActiveSpan.Context.TraceId) }))
//            {
//                _tracer.ActiveSpan.SetTag(Tags.Error, true);

//                if (args.MessageProperties.Headers.TryGetValue(MessageKeyHeader, out var bytes) && bytes is byte[] value)
//                {
//                    var key = Encoding.UTF8.GetString(value);

//                    _logger.LogError("Message returned {Exchange} {RoutingKey} reason {ReturnReason} {Key}",
//                                     args.MessageReturnedInfo.Exchange,
//                                     args.MessageReturnedInfo.RoutingKey,
//                                     args.MessageReturnedInfo.ReturnReason,
//                                     key);

//                    MessageReturned?.Invoke(new MessageReturnedEventArgs(key));
//                }
//                else
//                {
//                    _logger.LogError("Can not get error message filename");
//                }
//            }
//        }

//        public void Dispose()
//        {
//            if (_bus != null)
//            {
//                _bus.Advanced.MessageReturned -= OnMessageReturned;
//                _bus.Dispose();
//            }

//            _isInitialized = false;
//        }

//        public void RegisterIncomingType<T>(string routingKey, QueueSettings? queueSettings = null)
//        {
//            RegisterType<T>(routingKey, _incomingRoutingKeysDictionary, queueSettings);
//        }

//        public void RegisterOutgoingType<T>(string routingKey, QueueSettings? queueSettings = null)
//        {
//            RegisterType<T>(routingKey, _outgoingRoutingKeysDictionary, queueSettings);
//        }

//        private void RegisterType<T>(string routingKey, Dictionary<Type, string> routingKeysDictionary, QueueSettings? queueSettings = null)
//        {
//            var type = typeof(T);

//            if (routingKeysDictionary.ContainsKey(type))
//                throw new InvalidOperationException($"Type {type} already registered with routing key {routingKeysDictionary[type]}");

//            var settings = queueSettings ?? new QueueSettings { RoutingKey = routingKey };

//            ValidateSettings(settings);

//            routingKeysDictionary.Add(type, routingKey);
//            _queueSettingsDictionary.Add(routingKey, settings);
//        }

//        private async Task OnMessage<TMessage>(IMessage<TMessage> message, Func<TMessage, Task> processMessage)
//        {
//            var stringDictionary = message.Properties.Headers.Where(x => x.Value.GetType() == typeof(byte[])).ToDictionary(x => x.Key, x => Encoding.UTF8.GetString((byte[])x.Value));
//            var textMapExtractAdapter = new TextMapExtractAdapter(stringDictionary);
//            var spanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, textMapExtractAdapter);

//            using (_tracer.BuildSpan(nameof(OnMessage)).AddReference(References.FollowsFrom, spanContext).StartActive(true))
//            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>(nameof(_tracer.ActiveSpan.Context.TraceId), _tracer.ActiveSpan.Context.TraceId) }))
//            {
//                var tryCount = 0;

//                //TODO надо фильтровать ошибки базы, с3 и реббита, но тогда цикл должен быть выше по абстракции
//                while (true)
//                    try
//                    {
//                        await processMessage(message.Body).ConfigureAwait(false);
//                        break;
//                    }
//                    catch (ProcessMessageException)
//                    {
//                        throw;
//                    }
//                    catch (Exception e)
//                    {
//                        tryCount++;
//                        _logger.LogWarning(e, $"Process error, try count {tryCount}");

//                        if (tryCount >= 5)
//                            throw;
//                    }
//            }
//        }

//        private static MessageProperties GetMessageProperties()
//        {
//            return new MessageProperties
//            {
//                //TODO: Вернуть, когда перейдем на netcore или netstandard2.1 
//                //ContentType = MediaTypeNames.Application.Json,
//                ContentType = "application/json",
//                DeliveryMode = 2
//            };
//        }

//        private static async Task InitializeQueue(IBus bus, IExchange exchange, QueueSettings queueSettings)
//        {
//            var queue = await bus.Advanced.QueueDeclareAsync($"{exchange.Name}.{queueSettings.RoutingKey}",
//                                                             perQueueMessageTtl: queueSettings.QueueMessageTtl,
//                                                             deadLetterRoutingKey: queueSettings.DeadLetterRoutingKey,
//                                                             deadLetterExchange: queueSettings.DeadLetterExchange).ConfigureAwait(false);


//            await bus.Advanced.BindAsync(exchange, queue, queueSettings.RoutingKey).ConfigureAwait(false);
//        }

//        private static bool IsQueueNameHasUnsupportedCharacters(string queueName)
//        {
//            return queueName.ToCharArray().Where(x => x != '_' && x != '.').All(char.IsLetterOrDigit) == false;
//        }

//        private static void ValidateSettings(RabbitSettings settings)
//        {
//            var errors = new List<string>();

//            var errorMessage = new Func<string, string, string>((name, value) => $"{name} must not contains any unsupported characters ({value} = {value})");

//            if (IsQueueNameHasUnsupportedCharacters(settings.ExchangeName))
//                errors.Add(errorMessage(nameof(settings.ExchangeName), settings.ExchangeName));

//            if (string.IsNullOrEmpty(settings.ErrorRoutingKey) == false && IsQueueNameHasUnsupportedCharacters(settings.ErrorRoutingKey))
//                errors.Add(errorMessage(nameof(settings.ErrorRoutingKey), settings.ErrorRoutingKey));

//            if (string.IsNullOrEmpty(settings.IncomingQueue) == false && IsQueueNameHasUnsupportedCharacters(settings.IncomingQueue))
//                errors.Add(errorMessage(nameof(settings.IncomingQueue), settings.IncomingQueue));

//            if (errors.Count > 0)
//                throw new InvalidOperationException(string.Join(", ", errors));
//        }

//        private static void ValidateSettings(QueueSettings settings)
//        {
//            var errors = new List<string>();

//            var errorMessage = new Func<string, string, string>((name, value) => $"{name} must not contains any unsupported characters ({value} = {value})");

//            if (IsQueueNameHasUnsupportedCharacters(settings.RoutingKey))
//                errors.Add(errorMessage(nameof(settings.RoutingKey), settings.RoutingKey));

//            if (string.IsNullOrEmpty(settings.DeadLetterExchange) == false && IsQueueNameHasUnsupportedCharacters(settings.DeadLetterExchange))
//                errors.Add(errorMessage(nameof(settings.DeadLetterExchange), settings.DeadLetterExchange));

//            if (string.IsNullOrEmpty(settings.DeadLetterRoutingKey) == false && IsQueueNameHasUnsupportedCharacters(settings.DeadLetterRoutingKey))
//                errors.Add(errorMessage(nameof(settings.DeadLetterRoutingKey), settings.DeadLetterRoutingKey));

//            if (errors.Count > 0)
//                throw new InvalidOperationException(string.Join(", ", errors));
//        }
//    }

//    public class QueueSettings
//    {
//        public string DeadLetterRoutingKey { get; set; }

//        public string RoutingKey { get; set; }

//        public string DeadLetterExchange { get; set; }

//        public string QueueMessageTtl { get; set; }
//    }
//}