namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public class EventItem
    {
        public string Name { get; }

        public object? Value { get; }

        public string Description { get; }

        public EventItem(string name, object? value, string description)
        {
            Name = name;
            Value = value;
            Description = description;
        }
    }
}