using System;
using Byndyusoft.Messaging.RabbitMq.OpenTelemetry.Base;

namespace Byndyusoft.Messaging.RabbitMq.OpenTelemetry
{
    public class RabbitMqInstrumentation : IDisposable
    {
        private readonly Func<string, object?, object?, bool> _isEnabled = (_, _, _) => true;

        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RabbitMqInstrumentation(RabbitMqListener listener)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(listener, _isEnabled);
            _diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _diagnosticSourceSubscriber.Dispose();
        }
    }
}