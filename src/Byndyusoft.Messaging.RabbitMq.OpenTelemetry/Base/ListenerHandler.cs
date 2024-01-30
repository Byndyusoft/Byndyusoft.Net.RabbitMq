namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Base
{
    /// <summary>
    /// ListenerHandler base class.
    /// </summary>
    public abstract class ListenerHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListenerHandler"/> class.
        /// </summary>
        /// <param name="sourceName">The name of the <see cref="ListenerHandler"/>.</param>
        protected ListenerHandler(string sourceName)
        {
            SourceName = sourceName;
        }

        /// <summary>
        /// Gets the name of the <see cref="ListenerHandler"/>.
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Method called for an event which does not have 'Start', 'Stop' or 'Exception' as suffix.
        /// </summary>
        /// <param name="name">Custom name.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnEventWritten(string name, object? payload)
        {
        }
    }
}