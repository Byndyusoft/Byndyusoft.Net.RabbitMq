using System;
using System.Collections.Generic;
using System.Data.Common;
using Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties;

namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString
{
    public class RabbitMqConnectionStringBuilder : DbConnectionStringBuilder
    {
        private static readonly Dictionary<string, RabbitMqConnectionStringProperty> _properties;

        static RabbitMqConnectionStringBuilder()
        {
            _properties = new Dictionary<string, RabbitMqConnectionStringProperty>(StringComparer.CurrentCultureIgnoreCase)
            {
                {"virtualHost", RabbitMqConnectionStringProperty.String},
                {"username", RabbitMqConnectionStringProperty.String},
                {"password", RabbitMqConnectionStringProperty.String},
                {"requestedHeartbeat", RabbitMqConnectionStringProperty.TimeSpan},
                {"timeout", RabbitMqConnectionStringProperty.TimeSpan},
                {"prefetchcount", RabbitMqConnectionStringProperty.Ushort},
                {"name", RabbitMqConnectionStringProperty.String},
                {"publisherConfirms", RabbitMqConnectionStringProperty.Boolean},
                {"host", RabbitMqConnectionStringProperty.Host},
                {"requestedConnectionTimeout", RabbitMqConnectionStringProperty.TimeSpan}
            };
        }

        public const string DefaultVirtualHost = "/";
        public const string DefaultUserName = "guest";
        public const string DefaultPassword = "guest";
        public const ushort DefaultPrefetchCount = 50;
        public const bool DefaultPublisherConfirms = false;
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DefaultRequestedHeartbeat = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DefaultRequestedConnectionTimeout = TimeSpan.FromSeconds(30);

        public RabbitMqConnectionStringBuilder()
        {
            Hosts = new List<RabbitMqEndpoint>(new[] { new RabbitMqEndpoint("localhost") });
        }

        /// <summary>
        ///     Virtual host to connect to
        /// </summary>
        public string VirtualHost
        {
            get => (string)(this["virtualHost"] ?? DefaultVirtualHost);
            set => this["virtualHost"] = value;
        }

        /// <summary>
        ///     UserName used to connect to the broker
        /// </summary>
        public string UserName
        {
            get => (string)(this["username"] ?? DefaultUserName);
            set => this["username"] = value;
        }

        /// <summary>
        ///     Password used to connect to the broker
        /// </summary>
        public string Password
        {
            get => (string)(this["password"] ?? DefaultPassword);
            set => this["password"] = value;
        }

        /// <summary>
        ///     Heartbeat interval (default is 10 seconds)
        /// </summary>
        public TimeSpan RequestedHeartbeat
        {
            get => (TimeSpan)(this["requestedHeartbeat"] ?? DefaultRequestedHeartbeat);
            set => this["requestedHeartbeat"] = value;
        }

        /// <summary>
        ///     Prefetch count (default is 50)
        /// </summary>
        public ushort PrefetchCount
        {
            get => (ushort)(this["prefetchCount"] ?? DefaultPrefetchCount);
            set => this["prefetchCount"] = value;
        }

        /// <summary>
        ///     Operations timeout (default is 10s)
        /// </summary>
        public TimeSpan Timeout
        {
            get => (TimeSpan)(this["timeout"] ?? DefaultTimeout);
            set => this["timeout"] = value;
        }

        /// <summary>
        ///     Enables publisher confirms (default is false)
        /// </summary>
        public bool PublisherConfirms
        {
            get => (bool)(this["publisherConfirms"] ?? DefaultPublisherConfirms);
            set => this["publisherConfirms"] = value;
        }

        /// <summary>
        ///     List of hosts to use for the connection
        /// </summary>
        public IList<RabbitMqEndpoint> Hosts
        {
            get => (IList<RabbitMqEndpoint>)(this["host"] ?? new List<RabbitMqEndpoint>());
            set => this["host"] = value;
        }

        /// <summary>
        ///     Timeout setting for connection attempts.
        /// </summary>
        public TimeSpan RequestedConnectionTimeout
        {
            get => (TimeSpan)(this["requestedConnectionTimeout"] ?? DefaultRequestedConnectionTimeout);
            set => this["requestedConnectionTimeout"] = value;
        }

        public override object? this[string keyword]
        {
            get
            {
                var property = GetProperty(keyword);
                if (TryGetValue(keyword, out var value))
                    return property.CastToValue((string)value!);
                return null!;
            }
            set
            {
                var property = GetProperty(keyword);
                try
                {
                    base[keyword] = value is string str ? str : property.CastToString(value);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(e.Message, keyword);
                }
            }
        }

        private RabbitMqConnectionStringProperty GetProperty(string propertyName)
        {
            if (_properties.TryGetValue(propertyName, out var property))
                return property;
            throw new InvalidOperationException($"Property {propertyName} isn't supported");
        }
    }
}
