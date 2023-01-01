using System;
using System.Globalization;

namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal class RabbitMqConnectionStringTimeSpanProperty : RabbitMqConnectionStringProperty
    {
        public override string CastToString(object value) => ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture);

        public override object CastToValue(string value) => System.TimeSpan.FromSeconds(int.Parse(value));
    }
}