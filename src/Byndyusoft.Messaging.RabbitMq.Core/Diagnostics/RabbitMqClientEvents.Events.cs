using System;
using System.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Consts;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public class RabbitMqClientEvents
    {
        private static readonly DiagnosticListener EventLogger = new(DiagnosticNames.RabbitMq);

        public static void OnMessagePublishing(RabbitMqMessage message)
        {
            if (EventLogger.IsEnabled(EventNames.MessagePublishing))
                EventLogger.Write(EventNames.MessagePublishing, message);
        }

        public static void OnMessageReturned(ReturnedRabbitMqMessage message)
        {
            if (EventLogger.IsEnabled(EventNames.MessageReturned))
                EventLogger.Write(EventNames.MessageReturned, message);
        }

        public static void OnMessageGot(ReceivedRabbitMqMessage? message)
        {
            if (EventLogger.IsEnabled(EventNames.MessageGot))
                EventLogger.Write(EventNames.MessageGot, message);
        }

        public static void OnMessageReplied(ReceivedRabbitMqMessage message)
        {
            if (EventLogger.IsEnabled(EventNames.MessageReplied))
                EventLogger.Write(EventNames.MessageReplied, message);
        }

        public static void OnMessageConsumed(ConsumeResult result)
        {
            if (EventLogger.IsEnabled(EventNames.MessageConsumed))
                EventLogger.Write(EventNames.MessageConsumed, result);
        }

        public static void OnUnhandledException(Exception exception)
        {
            if (EventLogger.IsEnabled(EventNames.UnhandledException))
                EventLogger.Write(EventNames.UnhandledException, exception);
        }
    }
}