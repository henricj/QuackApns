using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.Network
{
    public class BufferedWriter
    {
        MemoryStream _bufferStream;
        MemoryStream _writeStream;
        Task _writeTask;
        long _writeTotal;

        public BufferedWriter(int bufferSize)
        {
            _bufferStream = new MemoryStream(bufferSize);
            _writeStream = new MemoryStream(bufferSize);
        }

        public long BytesWritten
        {
            get { return Interlocked.Read(ref _writeTotal); }
        }

        public int BytesRemaining
        {
            get { return _bufferStream.Capacity - (int)_bufferStream.Length; }
        }

        public Stream BufferStream
        {
            get { return _bufferStream; }
        }

        public async Task FlushBufferAsync(Stream stream, CancellationToken cancellationToken)
        {
            //Debug.WriteLine("Writing {0:F2}k", bufferStream.Length / 1024.0);

            if (null != _writeTask)
            {
                await _writeTask.ConfigureAwait(false);
                _writeTask = null;
            }

            FlushBuffer(stream, cancellationToken);
        }

        public bool TryFlushBuffer(Stream stream, CancellationToken cancellationToken)
        {
            if (null != _writeTask && !_writeTask.IsCompleted)
                return false;

            FlushBuffer(stream, cancellationToken);

            return true;
        }

        void FlushBuffer(Stream stream, CancellationToken cancellationToken)
        {
            var length = (int)_bufferStream.Length;

            if (null != _writeTask && !_writeTask.IsCompleted)
                throw new InvalidOperationException("The write task is still in use");

            _writeTask = stream.WriteAsync(_bufferStream.GetBuffer(), 0, length, cancellationToken);

            var incrementTask = _writeTask.ContinueWith(t => Interlocked.Add(ref _writeTotal, length),
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

            var tmp = _bufferStream;
            _bufferStream = _writeStream;
            _writeStream = tmp;

            _bufferStream.SetLength(0);
        }

        public async Task WaitAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (null != _writeTask)
            {
                await _writeTask.ConfigureAwait(false);

                _writeTask = null;
            }

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}