using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public class RabbitMqClientActivitySourceEvents
        {
            private static readonly DiagnosticListener EventLogger = new(DiagnosticNames.RabbitMq);

            public void MessagePublishing(Activity? activity, RabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessagePublishing))
                    EventLogger.Write(EventNames.MessagePublishing, message);
            }

            public void MessageReturned(Activity? activity, ReturnedRabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageReturned))
                    EventLogger.Write(EventNames.MessageReturned, message);
            }

            public void MessageGot(Activity? activity, ReceivedRabbitMqMessage? message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageGot))
                    EventLogger.Write(EventNames.MessageGot, message);
            }

            public void MessageReplied(Activity? activity, ReceivedRabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageReplied))
                    EventLogger.Write(EventNames.MessageReplied, message);
            }

            public void MessageConsumed(Activity? activity, ReceivedRabbitMqMessage _, ConsumeResult result)
            {
                if (EventLogger.IsEnabled(EventNames.MessageConsumed))
                    EventLogger.Write(EventNames.MessageConsumed, result);
            }
        }
    }
}