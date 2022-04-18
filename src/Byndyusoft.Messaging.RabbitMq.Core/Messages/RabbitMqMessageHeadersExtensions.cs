using System;
using System.Collections.Generic;

namespace Byndyusoft.Messaging.RabbitMq.Core.Messages
{
    public static class RabbitMqMessageHeadersExtensions
    {
        public static void SetRetryCount(this IDictionary<string, object?> headers, long retryCount)
        {
            var list = headers.GetValue("x-death") as List<object>;
            if (list is null) headers["x-death"] = list = new List<object>();

            if (list.Count == 0) list.Add(new Dictionary<string, object>());

            var dic = (Dictionary<string, object>) list[0];
            dic["count"] = retryCount;
        }

        public static long? GetRetryCount(this IDictionary<string, object?> headers)
        {
            var list = headers.GetValue("x-death") as List<object>;
            if (list is null || list.Count == 0) return null;

            var dic = (Dictionary<string, object?>) list[0];
            return dic.GetValue("count") as long?;
        }

        public static void SetException(this IDictionary<string, object?> headers, Exception? exception)
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

        public static void RemoveRetryData(this IDictionary<string, object?> headers)
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