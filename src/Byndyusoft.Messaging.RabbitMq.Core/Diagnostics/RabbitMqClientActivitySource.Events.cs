using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public partial class RabbitMqClientActivitySource
    {
        public class RabbitMqClientActivitySourceEvents
        {
            public void MessagePublishing(RabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessagePublishing))
                    EventLogger.Write(EventNames.MessagePublishing, message);
            }

            public void MessageReturned(ReturnedRabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageReturned))
                    EventLogger.Write(EventNames.MessageReturned, message);
            }

            public void MessageGot(ReceivedRabbitMqMessage? message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageGot))
                    EventLogger.Write(EventNames.MessageGot, message);
            }

            public void MessageReplied(ReceivedRabbitMqMessage message)
            {
                if (EventLogger.IsEnabled(EventNames.MessageReplied))
                    EventLogger.Write(EventNames.MessageReplied, message);
            }

            public void MessageConsumed(ConsumeResult result)
            {
                if (EventLogger.IsEnabled(EventNames.MessageConsumed))
                    EventLogger.Write(EventNames.MessageConsumed, result);
            }
        }
    }
}