using System;
using System.Collections.Generic;
using System.Linq;

namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal class RabbitMqConnectionStringHostProperty : RabbitMqConnectionStringProperty
    {
        public override object CastToValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<RabbitMqEndpoint>();

            return value.Split(",").Select(RabbitMqEndpoint.Parse).ToArray();
        }

        public override string CastToString(object value)
        {
            return string.Join(",", (IEnumerable<RabbitMqEndpoint>)value);
        }
    }
}