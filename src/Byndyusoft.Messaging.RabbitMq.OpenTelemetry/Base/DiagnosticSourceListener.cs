using System;
using System.Collections.Generic;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Base
{
    internal sealed class DiagnosticSourceListener : IObserver<KeyValuePair<string, object?>>
    {
        private readonly ListenerHandler _handler;

        public DiagnosticSourceListener(ListenerHandler handler)
        {
            Preconditions.CheckNotNull(handler, "Handler is null");

            _handler = handler;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            try
            {
                _handler.OnEventWritten(value.Key, value.Value);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}