using System.Diagnostics;

namespace Byndyusoft.Net.RabbitMq.Extensions.Middlewares.Metrics
{
    /// <summary>
    ///     Extensions for adding message type label on metrics
    /// </summary>
    public static class ActivityExtensions
    {
        /// <summary>
        ///    Name for baggage item with message type value
        /// </summary>
        private static string MessageTypeBaggageItemName = "MessageType";

        /// <summary>
        ///     Sets message type for activity
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <param name="messageType">Name of message type</param>
        public static void SetMessageType(this Activity activity, string messageType)
        {
            activity.AddBaggage(MessageTypeBaggageItemName, messageType);
        }

        /// <summary>
        ///     Returns message type of activity
        /// </summary>
        /// <param name="activity">Activity</param>
        public static string? GetMessageType(this Activity activity)
        {
            return activity.GetBaggageItem(MessageTypeBaggageItemName);
        }
    }
}