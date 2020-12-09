using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byndyusoft.Net.RabbitMq
{
    public class PersistentConnection : IDisposable
    {
        private readonly ILogger<RabbitMqProducer> _logger;
        private readonly object _mutex = new object();
        private readonly RabbitSettings _rabbitSettings;
        private volatile bool _disposed;
        private volatile IAutorecoveringConnection _connection;

        public PersistentConnection(
            IOptions<RabbitSettings> rabbitSettings,
            ILogger<RabbitMqProducer> logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _rabbitSettings = rabbitSettings.Value ?? throw new ArgumentException(nameof(rabbitSettings));
        }

        public IModel CreateChannel()
        {
            var connection = _connection;
            if (connection == null)
                lock (_mutex)
                {
                    connection = _connection ??= Connect();
                }

            if (!connection.IsOpen)
                throw new Exception("Attempt to create a channel while rabbit was disconnected.");
            
            var channel = connection.CreateModel();
            channel.BasicQos(0, 600, false);

            return channel;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _connection?.Dispose();
            _disposed = true;
        }

        private IAutorecoveringConnection Connect()
        {
            var connection = new ConnectionFactory
            {
                HostName = _rabbitSettings.HostName,
                UserName = _rabbitSettings.UserName,
                Password = _rabbitSettings.Password,
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true
            }.CreateConnection() as IAutorecoveringConnection;

            if (connection == null)
                throw new NotSupportedException("Non-recoverable connection is not supported");

            _logger.LogInformation("Rabbit's connection created");

            connection.ConnectionShutdown += OnConnectionShutdown;
            connection.ConnectionBlocked += OnConnectionBlocked;
            connection.ConnectionUnblocked += OnConnectionUnblocked;
            connection.RecoverySucceeded += OnConnectionRecovered;

            return connection;
        }

        private void OnConnectionRecovered(object sender, EventArgs e)
        {
            var connection = (IConnection) sender;
            _logger.LogInformation("Rabbit reconnected to broker {host}:{port}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );
        }

        private void OnConnectionUnblocked(object sender, EventArgs e)
        {
            _logger.LogInformation("Rabbit's connection unblocked");
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogInformation("Rabbit's connection blocked with reason {reason}", e.Reason);
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            var connection = (IConnection) sender;
            _logger.LogInformation("Rabbit disconnected from broker {host}:{port} because of {reason}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port,
                e.ReplyText);
        }
    }
}