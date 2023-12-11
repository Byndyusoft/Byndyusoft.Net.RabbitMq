using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics.Base
{
    internal sealed class DiagnosticSourceSubscriber : IDisposable, IObserver<DiagnosticListener>
    {
        private readonly List<IDisposable> _listenerSubscriptions;
        private readonly Func<string, ListenerHandler> _handlerFactory;
        private readonly Func<DiagnosticListener, bool> _diagnosticSourceFilter;
        private readonly Func<string, object?, object?, bool>? _isEnabledFilter;
        private long _disposed;
        private IDisposable? _allSourcesSubscription;

        public DiagnosticSourceSubscriber(
            ListenerHandler handler,
            Func<string, object?, object?, bool>? isEnabledFilter)
            : this(_ => handler, value => handler.SourceName == value.Name, isEnabledFilter)
        {
        }

        public DiagnosticSourceSubscriber(
            Func<string, ListenerHandler> handlerFactory,
            Func<DiagnosticListener, bool> diagnosticSourceFilter,
            Func<string, object?, object?, bool>? isEnabledFilter)
        {
            Preconditions.CheckNotNull(handlerFactory, "Handler Factory is null");

            _listenerSubscriptions = new List<IDisposable>();
            _handlerFactory = handlerFactory;
            _diagnosticSourceFilter = diagnosticSourceFilter;
            _isEnabledFilter = isEnabledFilter;
        }

        public void Subscribe()
        {
            _allSourcesSubscription ??= DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnNext(DiagnosticListener value)
        {
            if (Interlocked.Read(ref _disposed) == 0 && _diagnosticSourceFilter(value))
            {
                var handler = _handlerFactory(value.Name);
                var listener = new DiagnosticSourceListener(handler);
                var subscription = _isEnabledFilter == null 
                    ? value.Subscribe(listener) 
                    : value.Subscribe(listener, _isEnabledFilter);

                lock (_listenerSubscriptions)
                {
                    _listenerSubscriptions.Add(subscription);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            lock (_listenerSubscriptions)
            {
                foreach (var listenerSubscription in _listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                _listenerSubscriptions.Clear();
            }

            _allSourcesSubscription?.Dispose();
            _allSourcesSubscription = null;
        }
    }
}