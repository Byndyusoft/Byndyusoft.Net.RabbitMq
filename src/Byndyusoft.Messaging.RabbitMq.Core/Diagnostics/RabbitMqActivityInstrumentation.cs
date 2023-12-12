using System;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Base;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public class RabbitMqActivityInstrumentation : IDisposable
    {
        private readonly Func<string, object?, object?, bool> _isEnabled = (_, _, _) => true;

        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RabbitMqActivityInstrumentation(RabbitMqActivityListener listener)
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