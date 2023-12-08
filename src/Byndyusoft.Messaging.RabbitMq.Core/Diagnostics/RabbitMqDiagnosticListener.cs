using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Diagnostics
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
            if (_handler.SupportsNullActivity == false && Activity.Current == null)
            {
                return;
            }

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

    /// <summary>
    /// ListenerHandler base class.
    /// </summary>
    public abstract class ListenerHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListenerHandler"/> class.
        /// </summary>
        /// <param name="sourceName">The name of the <see cref="ListenerHandler"/>.</param>
        protected ListenerHandler(string sourceName)
        {
            SourceName = sourceName;
            SupportsNullActivity = false;
        }

        /// <summary>
        /// Gets the name of the <see cref="ListenerHandler"/>.
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ListenerHandler"/> supports NULL <see cref="Activity"/>.
        /// </summary>
        public virtual bool SupportsNullActivity { get; }

        /// <summary>
        /// Method called for an event which does not have 'Start', 'Stop' or 'Exception' as suffix.
        /// </summary>
        /// <param name="name">Custom name.</param>
        /// <param name="payload">An object that represent the value being passed as a payload for the event.</param>
        public virtual void OnEventWritten(string name, object? payload)
        {
        }
    }

    public class RabbitMqSourceSubscriber : IDisposable
    {
        private readonly Func<string, object?, object?, bool> _isEnabled = (_, _, _) => true;

        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RabbitMqSourceSubscriber(RabbitMqListener rabbitMqListener)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(rabbitMqListener, _isEnabled);
            _diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._diagnosticSourceSubscriber?.Dispose();
        }
    }

    public class RabbitMqListener : ListenerHandler
    {
        private const string DiagnosticSourceName = "Byndyusoft.RabbitMq";

        public RabbitMqListener() 
            : base(DiagnosticSourceName)
        {
        }

        public override bool SupportsNullActivity => true;

        public override void OnEventWritten(string name, object? payload)
        {
        }
    }
}