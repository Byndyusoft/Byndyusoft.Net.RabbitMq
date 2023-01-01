namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal class RabbitMqConnectionStringStringProperty : RabbitMqConnectionStringProperty
    {
        public override object CastToValue(string value) => value;
    }
}