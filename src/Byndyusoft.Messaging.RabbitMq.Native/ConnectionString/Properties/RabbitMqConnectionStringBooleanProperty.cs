namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal class RabbitMqConnectionStringBooleanProperty : RabbitMqConnectionStringProperty
    {
        public override string CastToString(object value) => ((bool)value).ToString().ToLower();

        public override object CastToValue(string value) => bool.Parse(value);
    }
}