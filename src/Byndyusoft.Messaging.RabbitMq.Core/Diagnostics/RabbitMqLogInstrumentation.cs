using System;
using Byndyusoft.Messaging.RabbitMq.Diagnostics.Base;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
{
    public class RabbitMqLogInstrumentation : IDisposable
    {
        private readonly Func<string, object?, object?, bool> _isEnabled = (_, _, _) => true;

        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RabbitMqLogInstrumentation(RabbitMqLogListener rabbitMqLogListener)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(rabbitMqLogListener, _isEnabled);
            _diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _diagnosticSourceSubscriber.Dispose();
        }
    }
}