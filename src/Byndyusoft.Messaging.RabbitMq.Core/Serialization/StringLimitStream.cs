using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Byndyusoft.Messaging.RabbitMq.Serialization
{
    [ExcludeFromCodeCoverage]
    internal class StringLimitStream : Stream
    {
        private readonly int? _lengthLimit;
        private MemoryStream? _memory;
        private bool _oversized;

        public StringLimitStream(int? lengthLimit)
        {
            _lengthLimit = lengthLimit;
            _memory = new MemoryStream();
        }

        public override bool CanRead => Inner.CanRead;

        public override bool CanSeek => Inner.CanSeek;

        public override bool CanWrite => Inner.CanWrite;

        public override long Length => Inner.Length;

        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        private MemoryStream Inner => _memory ?? throw new ObjectDisposedException(nameof(StringLimitStream));

        public override void Flush()
        {
            Inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_oversized)
                return;

            var inner = Inner;

            if (_lengthLimit != null && inner.Length + count > _lengthLimit)
            {
                _oversized = true;
                count = _lengthLimit.Value - (int) inner.Length;
            }

            inner.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memory?.Dispose();
                _memory = null;
            }

            base.Dispose(disposing);
        }

        public string GetString()
        {
            var str = Encoding.UTF8.GetString(Inner.ToArray());
            return _oversized ? $"{str}..." : str;
        }
    }
}