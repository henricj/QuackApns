// Copyright (c) 2014 Henric Jungheim <software@henric.org>
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using QuackApns.Network;
using QuackApns.Utility;

namespace QuackApns
{
    public class ApnsPushClient : INetConnectionHandler
    {
        const int BufferSize = 1024 * 1024;
        readonly ApnsNotificationWriter _writer = new ApnsNotificationWriter();
        MemoryStream _bufferStream = new MemoryStream(BufferSize);
        int _identifier;
        MemoryStream _writeStream = new MemoryStream(BufferSize);
        Task _writeTask;
        long _writeTotal;
        long _messageCount;

        readonly BufferBlock<ICollection<ApnsNotification>> _bufferBlock = new BufferBlock<ICollection<ApnsNotification>>();

        public long MessageCount { get { return Interlocked.Read(ref _messageCount); } }
        public long BytesWritten { get { return Interlocked.Read(ref _writeTotal); } }

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var total = 0L;

            var buffer = new byte[1024];

            try
            {
                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                total += count;

                // Parse response...

                stream.Close(); // Only errors are ever sent, so we stop
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Reader failed: " + ex.Message);
            }

            return total;
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            var writerBlock = new ActionBlock<ICollection<ApnsNotification>>(notifications => WriteNotificationsAsync(notifications, stream, cancellationToken),
                new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken, BoundedCapacity = 1 });

            using (_bufferBlock.LinkTo(writerBlock, new DataflowLinkOptions { PropagateCompletion = true }))
            {
                await writerBlock.Completion.ConfigureAwait(false);
            }

            await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

            if (null != _writeTask)
                await _writeTask.ConfigureAwait(false);

            return _writeTotal;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return TplHelpers.CompletedTask;
        }

        #endregion

        async Task WriteNotificationsAsync(ICollection<ApnsNotification> notifications, Stream stream, CancellationToken cancellationToken)
        {
            if (null == notifications || 0 == notifications.Count)
            {
                // The nulls are requests to flush.
                await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

                var flushSentinel = notifications as FlushSentinel;

                if (null == flushSentinel)
                    return;

                if (flushSentinel.IsBlocking)
                {
                    if (null != _writeTask)
                        await _writeTask.ConfigureAwait(false);

                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                flushSentinel.TrySetResult(true);

                return;
            }

            foreach (var notification in notifications)
            {
                notification.Identifier = ++_identifier;

                Interlocked.Increment(ref _messageCount);

                _writer.Write(_bufferStream, notification);

                if (_bufferStream.Length > BufferSize - 3072)
                    await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        async Task FlushBufferAsync(Stream stream, CancellationToken cancellationToken)
        {
            //Debug.WriteLine("Writing {0:F2}k", bufferStream.Length / 1024.0);

            if (null != _writeTask)
                await _writeTask.ConfigureAwait(false);

            var length = (int)_bufferStream.Length;

            _writeTask = stream.WriteAsync(_bufferStream.GetBuffer(), 0, length, cancellationToken);

            var incrementTask = _writeTask.ContinueWith(t => Interlocked.Add(ref _writeTotal, length),
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

            var tmp = _bufferStream;
            _bufferStream = _writeStream;
            _writeStream = tmp;

            _bufferStream.SetLength(0);
        }

        public bool Post(ICollection<ApnsNotification> notifications)
        {
            return _bufferBlock.Post(notifications);
        }

        public Task<bool> SendAsync(ICollection<ApnsNotification> notifications, CancellationToken cancellationToken)
        {
            return _bufferBlock.SendAsync(notifications, cancellationToken);
        }

        public void Push()
        {
            _bufferBlock.Post(null);
        }

        public async Task<bool> FlushAsync(bool blocking, CancellationToken cancellationToken)
        {
            var flushSentinel = new FlushSentinel(blocking);

            using (cancellationToken.Register(() => flushSentinel.TrySetCanceled()))
            {
                var posted = await _bufferBlock.SendAsync(flushSentinel, cancellationToken).ConfigureAwait(false);

                if (posted)
                    return await flushSentinel.Task.ConfigureAwait(false);
            }

            return false;
        }

        #region Nested type: FlushSentinel

        class FlushSentinel : TaskCompletionSource<bool>, ICollection<ApnsNotification>
        {
            public FlushSentinel(bool blocking)
            {
                IsBlocking = blocking;
            }

            public bool IsBlocking { get; private set; }

            #region ICollection<ApnsNotification> Members

            public int Count
            {
                get { return 0; }
            }

            void ICollection<ApnsNotification>.Add(ApnsNotification item)
            {
                throw new NotImplementedException();
            }

            void ICollection<ApnsNotification>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<ApnsNotification>.Contains(ApnsNotification item)
            {
                throw new NotImplementedException();
            }

            void ICollection<ApnsNotification>.CopyTo(ApnsNotification[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            bool ICollection<ApnsNotification>.IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            bool ICollection<ApnsNotification>.Remove(ApnsNotification item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<ApnsNotification> IEnumerable<ApnsNotification>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion
    }
}
