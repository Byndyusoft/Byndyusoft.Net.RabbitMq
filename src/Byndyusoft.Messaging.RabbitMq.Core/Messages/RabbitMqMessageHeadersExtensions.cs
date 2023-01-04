using System;
using System.Collections.Generic;

namespace Byndyusoft.Messaging.RabbitMq.Messages
{
    public static class RabbitMqMessageHeadersExtensions
    {
        public static ulong? GetRetryCount(this RabbitMqMessageHeaders headers)
        {
            var list = headers.GetValue("x-death") as List<object>;
            if (list is null || list.Count == 0) return null;

            var dic = (Dictionary<string, object?>) list[0];
            return (ulong?)(dic.GetValue("count") as long?);
        }

        public static void SetException(this RabbitMqMessageHeaders headers, Exception? exception)
        {
            if (exception is not null)
            {
                headers["exception-type"] = exception.GetType().FullName;
                headers["exception-message"] = exception.Message;
            }
            else
            {
                headers.Remove("exception-type");
                headers.Remove("exception-message");
            }
        }

        public static Exception? GetException(this RabbitMqMessageHeaders headers)
        {
            if (headers.TryGetValue("exception-message", out var message))
            {
                return message is not null
                    ? new Exception((string) message)
                    : new Exception();
            }

            return null;
        }

        public static void RemoveRetryData(this RabbitMqMessageHeaders headers)
        {
            headers.Remove("x-death");
            headers.Remove("x-first-death-exchange");
            headers.Remove("x-first-death-queue");
            headers.Remove("x-first-death-reason");
        }

        private static object? GetValue(this IDictionary<string, object?> headers, string key)
        {
            return headers.TryGetValue(key, out var value) ? value : null;
        }
    }
}