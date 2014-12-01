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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using QuackApns.Network;
using QuackApns.Utility;

namespace QuackApns
{
    public class ApnsPushConnection : INetConnectionHandler,
        IPropagatorBlock<ApnsNotification, ApnsNotification>,
        IReceivableSourceBlock<ApnsNotification>
    {
        const int BufferSize = 1024 * 1024;
        readonly Queue<WriteLog> _activeWrites = new Queue<WriteLog>();
        readonly BufferedWriter _bufferedWriter = new BufferedWriter(BufferSize);
        readonly BufferBlock<ApnsDevice> _deviceErrorBlock = new BufferBlock<ApnsDevice>();
        readonly ConcurrentQueue<Tuple<ApnsErrorCode, uint>> _errorResponse = new ConcurrentQueue<Tuple<ApnsErrorCode, uint>>();
        readonly BufferBlock<ApnsNotification> _inputBlock;
        readonly BufferBlock<ApnsNotification> _outputBlock;
        readonly ApnsNotificationWriter _writer = new ApnsNotificationWriter();

        uint _identifier;
        ApnsNotification _notification;
        long _notificationCount;
        WriteLog _pendingWrite;
        bool _readOk;
        BufferBlock<ApnsNotification> _retryBlock;

        public ApnsPushConnection()
        {
            _outputBlock = new BufferBlock<ApnsNotification>();
            _inputBlock = new BufferBlock<ApnsNotification>();
        }

        public long NotificationCount
        {
            get { return Interlocked.Read(ref _notificationCount); }
        }

        public long BytesWritten
        {
            get { return _bufferedWriter.BytesWritten; }
        }

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var total = 0L;
            var offset = 0;

            var buffer = new byte[256];

            try
            {
                for (; ; )
                {
                    var count = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);

                    if (count < 1)
                        break;

                    total += count;

                    offset += count;

                    var i = 0;
                    for (; i + 6 <= offset; i += 6)
                        ParseErrorResponse(buffer, i);

                    var remaining = offset - i;

                    if (remaining > 0)
                    {
                        Array.Copy(buffer, i, buffer, 0, remaining);
                        offset = remaining;
                    }
                    else
                        offset = 0;
                }

                if (0 == total)
                    _readOk = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Reader failed: " + ex.Message);
            }

            if (offset >= 6)
            {
                for (var i = 0; i + 6 <= offset; i += 6)
                    ParseErrorResponse(buffer, i);
            }
            else if (offset > 0)
            {
                Debug.WriteLine("Invalid error response: " + offset);
            }

            if (0 == total)
            {
                // We have a clean shutdown, so we will assume that everything
                // that has been written has been accepted.
                _errorResponse.Enqueue(null);
                this.Post(null);
            }

            return total;
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            var writerBlock = new ActionBlock<ApnsNotification>(notifications => WriteNotificationsAsync(notifications, stream, cancellationToken),
                new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken, BoundedCapacity = 1 });

            try
            {
                // First, we retry anything still up in the air.
                if (null != _retryBlock || _activeWrites.Count > 0 || null != _pendingWrite || null != _notification)
                {
                    var oldRetries = _retryBlock;

                    _retryBlock = RebuildRetryBlock(oldRetries);

                    _retryBlock.Complete();

                    using (_retryBlock.LinkTo(writerBlock))
                    {
                        await _retryBlock.Completion.ConfigureAwait(false);
                    }
                }

                // Now, process the normal input block...
                using (_inputBlock.LinkTo(writerBlock, new DataflowLinkOptions { PropagateCompletion = true }))
                {
                    await writerBlock.Completion.ConfigureAwait(false);
                }

                await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

                await _bufferedWriter.WaitAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    RetireCompletedWrites(_bufferedWriter.BytesWritten);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HandleErrorResponses failed: " + ex.Message);
                }
            }

            return _bufferedWriter.BytesWritten;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            if (!_errorResponse.IsEmpty)
                HandleErrorResponses();

            var bytesWritten = _bufferedWriter.BytesWritten;

            while (_activeWrites.Count > 0)
            {
                var writeLog = _activeWrites.Dequeue();

                if (_readOk || IsCompleted(writeLog, bytesWritten))
                    CompleteWrite(writeLog);
                else
                    FailWrite(writeLog);
            }

            if (null != _pendingWrite)
                FailWrite(_pendingWrite);

            _pendingWrite = null;

            if (null != _retryBlock && 0 != _retryBlock.Count)
            {
                _retryBlock.Complete();

                ApnsNotification apnsNotification;
                while (_retryBlock.TryReceive(null, out apnsNotification))
                {
                    FailNotification(apnsNotification);
                }

                _retryBlock = null;
            }

            _inputBlock.Complete();

            if (_inputBlock.Count > 0)
            {
                ApnsNotification apnsNotification;
                while (_inputBlock.TryReceive(null, out apnsNotification))
                {
                    FailNotification(apnsNotification);
                }
            }

            _outputBlock.Complete();

            _deviceErrorBlock.Complete();

            return TplHelpers.CompletedTask;
        }

        #endregion

        #region IPropagatorBlock<ApnsNotification,ApnsNotification> Members

        DataflowMessageStatus ITargetBlock<ApnsNotification>.OfferMessage(DataflowMessageHeader messageHeader, ApnsNotification messageValue, ISourceBlock<ApnsNotification> source, bool consumeToAccept)
        {
            return ((ITargetBlock<ApnsNotification>)_inputBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            _inputBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            ((IDataflowBlock)_inputBlock).Fault(exception);
        }

        public Task Completion
        {
            get { return _outputBlock.Completion; }
        }

        public IDisposable LinkTo(ITargetBlock<ApnsNotification> target, DataflowLinkOptions linkOptions)
        {
            return _outputBlock.LinkTo(target, linkOptions);
        }

        ApnsNotification ISourceBlock<ApnsNotification>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<ApnsNotification> target, out bool messageConsumed)
        {
            return ((ISourceBlock<ApnsNotification>)_outputBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<ApnsNotification>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<ApnsNotification> target)
        {
            return ((ISourceBlock<ApnsNotification>)_outputBlock).ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<ApnsNotification>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<ApnsNotification> target)
        {
            ((ISourceBlock<ApnsNotification>)_outputBlock).ReleaseReservation(messageHeader, target);
        }

        #endregion

        #region IReceivableSourceBlock<ApnsNotification> Members

        bool IReceivableSourceBlock<ApnsNotification>.TryReceive(Predicate<ApnsNotification> filter, out ApnsNotification item)
        {
            return _outputBlock.TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<ApnsNotification>.TryReceiveAll(out IList<ApnsNotification> items)
        {
            return _outputBlock.TryReceiveAll(out items);
        }

        #endregion

        BufferBlock<ApnsNotification> RebuildRetryBlock(BufferBlock<ApnsNotification> oldRetries)
        {
            // This could cause further retries, so first we make
            // room for a new retry block.

            var retryBlock = new BufferBlock<ApnsNotification>();

            while (_activeWrites.Count > 0)
            {
                var writeLog = _activeWrites.Dequeue();

                foreach (var notification in writeLog.Notifications)
                    retryBlock.Post(notification);
            }

            if (null != _pendingWrite)
            {
                foreach (var notification in _pendingWrite.Notifications)
                    retryBlock.Post(notification);

                _pendingWrite = null;
            }

            if (null != _notification)
            {
                retryBlock.Post(_notification);
                _notification = null;
            }

            if (null != oldRetries && 0 != oldRetries.Count)
            {
                oldRetries.Complete();

                ApnsNotification notification;

                while (oldRetries.TryReceive(null, out notification))
                    retryBlock.Post(notification);
            }

            return retryBlock;
        }

        void ParseErrorResponse(byte[] buffer, int offset)
        {
            if (8 != buffer[offset])
                throw new FormatException("Unknown error response message type: " + buffer[offset]);

            var status = buffer[offset + 1];

            var identifier = (uint)((buffer[offset + 2] << 24)
                                    | (buffer[offset + 3] << 16)
                                    | (buffer[offset + 4] << 8)
                                    | buffer[offset + 5]);

            _errorResponse.Enqueue(Tuple.Create((ApnsErrorCode)status, identifier));
            this.Post(null); // Wake the writer...
        }

        async Task WriteNotificationsAsync(ApnsNotification notification, Stream stream, CancellationToken cancellationToken)
        {
            _notification = notification;

            if (!_errorResponse.IsEmpty)
                HandleErrorResponses();

            if (null == notification || null == notification.Devices)
            {
                // The nulls are requests to flush.

                _notification = null;

                await HandleFlushNotificationAsync(notification, stream, cancellationToken).ConfigureAwait(false);

                return;
            }

            var devices = notification.Devices;

            if (0 == devices.Count)
            {
                _notification = null;
                return;
            }

            var flushThreshold = 128 + notification.Payload.Count;

            for (var i = notification.DeviceIndex; i < devices.Count; ++i)
            {
                var device = devices[i];

                device.Identifier = ++_identifier;

                Interlocked.Increment(ref _notificationCount);

                _writer.Write(_bufferedWriter.BufferStream, notification, device);

                if (_bufferedWriter.BytesRemaining < flushThreshold)
                    await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _notification = null;

            AddToPendingWrite(notification);

            if (!_errorResponse.IsEmpty)
                HandleErrorResponses();

            if (0 == _inputBlock.Count)
                TryFlushBuffer(stream, cancellationToken);
        }

        async Task<bool> HandleFlushNotificationAsync(ApnsNotification notification, Stream stream, CancellationToken cancellationToken)
        {
            await FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

            if (null == notification)
                return true;

            var flushSentinel = notification.Devices as FlushSentinel;

            if (null == flushSentinel)
                return true;

            if (flushSentinel.IsBlocking)
                await _bufferedWriter.WaitAsync(stream, cancellationToken).ConfigureAwait(false);

            flushSentinel.TrySetResult(true);
            return false;
        }

        void HandleErrorResponses()
        {
            Tuple<ApnsErrorCode, uint> errorResponse;
            while (_errorResponse.TryDequeue(out errorResponse))
            {
                if (null == errorResponse)
                {
                    // Accept everything...
                    while (_activeWrites.Count > 0)
                    {
                        var write = _activeWrites.Dequeue();

                        CompleteWrite(write);
                    }

                    continue;
                }

                var isError = ApnsErrorCode.NoError != errorResponse.Item1 && ApnsErrorCode.Shutdown != errorResponse.Item1;

                var identifier = errorResponse.Item2;

                Debug.WriteLine("HandleErrorResponse " + isError + " " + identifier);

                var foundIdentifier = false;

                while (_activeWrites.Count > 0)
                {
                    var write = _activeWrites.Dequeue();

                    if (foundIdentifier)
                    {
                        FailWrite(write);
                        continue;
                    }

                    foreach (var notification in write.Notifications)
                    {
                        if (null == notification)
                            continue;

                        if (foundIdentifier)
                        {
                            FailNotification(notification);
                            continue;
                        }

                        if (null == notification.Devices || 0 == notification.Devices.Count)
                            continue;

                        var lastDevice = notification.Devices.Last();

                        var difference = (int)(identifier - lastDevice.Identifier);

                        if (difference >= 0)
                        {
                            // All have been accepted
                            CompleteNotification(notification);
                        }
                        else
                        {
                            foundIdentifier = true;
                            CompletePartialNotification(notification, identifier, isError);
                        }
                    }
                }
            }
        }

        bool IsCompleted(ApnsDevice device, uint identifier)
        {
            var difference = (int)(identifier - device.Identifier);

            return difference >= 0;
        }

        void CompleteWrite(WriteLog writeLog)
        {
            Debug.WriteLine("CompleteWrite");

            foreach (var notification in writeLog.Notifications)
                CompleteNotification(notification);
        }

        void FailWrite(WriteLog write)
        {
            foreach (var notification in write.Notifications)
                FailNotification(notification);
        }

        void CompleteNotification(ApnsNotification notification)
        {
            notification.DeviceIndex = notification.Devices.Count;

            _outputBlock.Post(notification);
        }

        void CompletePartialNotification(ApnsNotification notification, uint identifier, bool isError)
        {
            UpdateNotificationDeviceIndex(notification, identifier, isError);

            _outputBlock.Post(notification);
        }

        void FailNotification(ApnsNotification notification)
        {
            _outputBlock.Post(notification);
        }

        bool UpdateNotificationDeviceIndex(ApnsNotification notification, uint identifier, bool isError)
        {
            var devices = notification.Devices;

            var found = false;

            for (var i = notification.DeviceIndex; i < devices.Count; ++i)
            {
                var device = devices[i];

                if (IsCompleted(device, identifier))
                    continue;

                notification.DeviceIndex = i + 1;
                found = true;

                if (isError && device.Identifier == identifier + 1)
                    _deviceErrorBlock.Post(device);

                break;
            }

            if (!found)
                notification.DeviceIndex = notification.Devices.Count;

            return found;
        }

        async Task FlushBufferAsync(Stream stream, CancellationToken cancellationToken)
        {
            var wasWritten = _bufferedWriter.BytesWritten;

            await _bufferedWriter.FlushBufferAsync(stream, cancellationToken);

            ActivatePendingWrites(wasWritten);
        }

        bool TryFlushBuffer(Stream stream, CancellationToken cancellationToken)
        {
            var wasWritten = _bufferedWriter.BytesWritten;

            if (!_bufferedWriter.TryFlushBuffer(stream, cancellationToken))
            {
                RetireCompletedWrites(wasWritten);

                return false;
            }

            ActivatePendingWrites(wasWritten);

            return true;
        }

        bool IsCompleted(WriteLog oldestWrite, long wasWritten)
        {
            // What should this do?  The only way to be sure is to get an ACK from
            // the other end.
            return oldestWrite.Position + 3 * BufferSize < wasWritten
                   || oldestWrite.SinceWrite.Elapsed > TimeSpan.FromSeconds(30)
                   || oldestWrite.NotificationCount + BufferSize / 100 + 10000 < _notificationCount;
        }

        void ActivatePendingWrites(long wasWritten)
        {
            Debug.WriteLine("ActivatePendingWrites " + wasWritten);

            RetireCompletedWrites(wasWritten);

            var write = _pendingWrite;

            if (null == write)
                return;

            _pendingWrite = null;

            write.NotificationCount = _notificationCount;
            write.Position = wasWritten;
            write.SinceWrite.Start();

            _activeWrites.Enqueue(write);
        }

        void RetireCompletedWrites(long written)
        {
            Debug.WriteLine("RetireCompletedWrites " + written);

            if (!_errorResponse.IsEmpty)
                HandleErrorResponses();

            while (_activeWrites.Count > 0)
            {
                var oldestWrite = _activeWrites.Peek();

                if (!IsCompleted(oldestWrite, written))
                    return;

                var writeLog = _activeWrites.Dequeue();

                Debug.Assert(ReferenceEquals(writeLog, oldestWrite), "Dequeue() doesn't match Peek()");

                CompleteWrite(writeLog);
            }
        }

        void AddToPendingWrite(ApnsNotification notification)
        {
            if (null == _pendingWrite)
                _pendingWrite = new WriteLog();

            _pendingWrite.Notifications.Add(notification);
        }

        public async Task<bool> FlushAsync(bool blocking, CancellationToken cancellationToken)
        {
            var flushSentinel = new FlushSentinel(blocking);

            using (cancellationToken.Register(() => flushSentinel.TrySetCanceled()))
            {
                var posted = await this.SendAsync(new ApnsNotification { Devices = flushSentinel }, cancellationToken).ConfigureAwait(false);

                if (posted)
                    return await flushSentinel.Task.ConfigureAwait(false);
            }

            return false;
        }

        #region Nested type: FlushSentinel

        class FlushSentinel : TaskCompletionSource<bool>, IReadOnlyList<ApnsDevice>
        {
            public FlushSentinel(bool blocking)
            {
                IsBlocking = blocking;
            }

            public bool IsBlocking { get; private set; }

            #region IReadOnlyList<ApnsDevice> Members

            public int Count
            {
                get { return 0; }
            }

            IEnumerator<ApnsDevice> IEnumerable<ApnsDevice>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public ApnsDevice this[int index]
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        #endregion

        #region Nested type: WriteLog

        class WriteLog
        {
            public readonly List<ApnsNotification> Notifications = new List<ApnsNotification>();
            public readonly Stopwatch SinceWrite = new Stopwatch();
            public long NotificationCount;
            public long Position;
        }

        #endregion
    }
}
