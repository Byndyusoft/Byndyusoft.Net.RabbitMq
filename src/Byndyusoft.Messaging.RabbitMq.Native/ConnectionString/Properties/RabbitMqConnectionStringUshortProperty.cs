using System.Globalization;

namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal class RabbitMqConnectionStringUshortProperty : RabbitMqConnectionStringProperty
    {
        public override object CastToValue(string value) => ushort.Parse(value, NumberStyles.Number);
    }
}