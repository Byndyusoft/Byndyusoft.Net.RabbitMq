namespace Byndyusoft.Messaging.RabbitMq.Native.ConnectionString.Properties
{
    internal abstract class RabbitMqConnectionStringProperty
    {
        public virtual string CastToString(object value) => value.ToString();

        public abstract object CastToValue(string value);

        public static readonly RabbitMqConnectionStringProperty String = new RabbitMqConnectionStringStringProperty();

        public static readonly RabbitMqConnectionStringProperty Boolean = new RabbitMqConnectionStringBooleanProperty();

        public static readonly RabbitMqConnectionStringProperty TimeSpan = new RabbitMqConnectionStringTimeSpanProperty();

        public static readonly RabbitMqConnectionStringProperty Ushort = new RabbitMqConnectionStringUshortProperty();

        public static readonly RabbitMqConnectionStringProperty Host = new RabbitMqConnectionStringHostProperty();
    }
}