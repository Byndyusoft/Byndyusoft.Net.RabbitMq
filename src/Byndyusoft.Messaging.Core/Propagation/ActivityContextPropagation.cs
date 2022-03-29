using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using Byndyusoft.Messaging.Abstractions;

namespace Byndyusoft.Messaging.Propagation
{
    internal static class ActivityContextPropagation
    {
        public static void SetContext(Activity? activity, QueueMessage message)
        {
            if (activity is null)
                return;

            var baggage = activity.Baggage.Where(x => x.Value is not null)
                .Select(pair => new NameValueHeaderValue(pair.Key, WebUtility.UrlEncode(pair.Value)).ToString())
                .ToArray();

            message.Headers[PropagationHeaderNames.TraceState] = activity.TraceStateString;
            message.Headers[PropagationHeaderNames.TraceParent] = activity.Id;
            message.Headers[PropagationHeaderNames.Baggage] = string.Join(";", baggage);
        }

        public static void ExtractContext(Activity? activity, ConsumedQueueMessage? message)
        {
            if (activity is null || message is null)
                return;

            if (activity.ParentId is not null)
                return;

            if (message.Headers[PropagationHeaderNames.TraceParent] is string parentId)
                activity.SetParentId(parentId);

            activity.TraceStateString = message.Headers[PropagationHeaderNames.TraceState] as string;

            if (message.Headers[PropagationHeaderNames.Baggage] is string baggageString)
                foreach (var item in baggageString.Split(';'))
                    if (NameValueHeaderValue.TryParse(item, out NameValueHeaderValue baggageItem))
                        activity.SetBaggage(baggageItem.Name, WebUtility.UrlDecode(baggageItem.Value));
        }
    }
}