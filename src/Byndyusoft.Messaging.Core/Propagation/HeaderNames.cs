namespace Byndyusoft.Messaging.Propagation
{
    internal static class PropagationHeaderNames
    {
        public const string TraceParent = "traceparent";
        public const string TraceState = "tracestate";
        public const string Baggage = "baggage";

        public static bool IsPropagationName(string headerName)
        {
            return string.Equals(headerName, TraceParent) ||
                   string.Equals(headerName, TraceState) ||
                   string.Equals(headerName, Baggage);
        }
    }
}