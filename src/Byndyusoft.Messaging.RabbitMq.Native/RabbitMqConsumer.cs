using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Native.Internal;
using Byndyusoft.Messaging.RabbitMq.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byndyusoft.Messaging.RabbitMq.Native
{
    internal class RabbitMqAsyncConsumer : Disposable
    {
        private IModel? _model;
        private readonly string _queueName;
        private readonly Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> _onMessage;
        private AsyncEventingBasicConsumer? _consumer;
        private readonly CancellationTokenSource _tokenSource = new ();

        public RabbitMqAsyncConsumer(
            IModel model,
            string queueName,
            bool? exclusive,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> onMessage)
        {
            _model = model;
            _queueName = queueName;
            _onMessage = onMessage;
            
            _consumer = new AsyncEventingBasicConsumer(model);
            _consumer.Received += ConsumerOnReceived;
            _consumer.Shutdown += ConsumerOnShutdown;
            _model.BasicConsume(_consumer, _queueName, exclusive: exclusive ?? false);
        }

        private Task ConsumerOnShutdown(object _, ShutdownEventArgs @event)
        {
            _tokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async Task ConsumerOnReceived(object _, BasicDeliverEventArgs args)
        {
            try
            {
                var consumedMessage = ReceivedRabbitMqMessageFactory.CreateReceivedMessage(_queueName, args);
                var consumeResult = await _onMessage(consumedMessage, _tokenSource.Token)
                    .ConfigureAwait(false);

                switch (consumeResult)
                {
                    case HandlerConsumeResult.Ack: 
                        _model!.BasicAck(args.DeliveryTag, false);
                        break;
                    case HandlerConsumeResult.RejectWithRequeue:
                        _model!.BasicNack(args.DeliveryTag, false, true);
                        break;
                    case HandlerConsumeResult.RejectWithoutRequeue:
                        _model!.BasicNack(args.DeliveryTag, false, false);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unexpected ConsumeResult Value={consumeResult}, Retry or Error value should be handled previously");
                }
            }
            catch
            {
                _model!.BasicNack(args.DeliveryTag, false, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) return;

            if (_consumer != null)
            {
                _consumer.Received -= ConsumerOnReceived;
                _consumer.Shutdown -= ConsumerOnShutdown;
                _consumer = null;
            }

            _tokenSource.Cancel();
            _model?.Dispose();
            _model = null;
        }
    }
}