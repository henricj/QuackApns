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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using QuackApns.Random;

namespace QuackApns.SqlServerRepository
{
    public sealed class SqlNotificationWriter : IDisposable
    {
        readonly BatchBlock<ApnsNotification> _bufferBlock = new BatchBlock<ApnsNotification>(20);
        readonly ActionBlock<ApnsNotification[]> _sqlWriterBlock;
        readonly TransformBlock<ApnsNotification, ApnsNotification> _timeoutBlock;
        readonly Timer _timeoutTimer;
        SqlServerConnection _connection;
        IRandomGenerator<ulong> _rng;

        public SqlNotificationWriter()
        {
            _sqlWriterBlock = new ActionBlock<ApnsNotification[]>((Func<ApnsNotification[], Task>)SqlWriteAsync);

            _timeoutBlock = new TransformBlock<ApnsNotification, ApnsNotification>(c =>
            {
                _timeoutTimer.Change(2500, Timeout.Infinite);

                return c;
            });

            _timeoutTimer = new Timer(state =>
            {
                var block = (BatchBlock<ApnsNotification>)state;

                Debug.WriteLine("Sql batch timeout");

                block.TriggerBatch();
            }, _bufferBlock, Timeout.Infinite, Timeout.Infinite);

            _timeoutBlock.Completion.ContinueWith(obj => { _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite); }, TaskContinuationOptions.ExecuteSynchronously);

            _timeoutBlock.LinkTo(_bufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _bufferBlock.LinkTo(_sqlWriterBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public ITargetBlock<ApnsNotification> TargetBlock
        {
            get { return _timeoutBlock; }
        }

        public Task Completion
        {
            get { return _sqlWriterBlock.Completion; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            var connection = _connection;

            if (null != connection)
            {
                _connection = null;
                connection.Dispose();
            }

            _timeoutTimer.Dispose();
        }

        #endregion

        async Task SqlWriteAsync(ApnsNotification[] completions)
        {
            Debug.WriteLine("SqlWriteAsync " + completions.Length);

            var delay = 123.0;
            var i = 0;

            for (var outerRetry = 0; outerRetry < 3; ++outerRetry)
            {
                using (var connection = await SqlServerConnection.ConnectAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    for (var innerRetry = 0; innerRetry < 2; ++innerRetry)
                    {
                        try
                        {
                            for (; i < completions.Length; ++i)
                            {
                                var completion = (SqlApnsNotification)completions[i];

                                if (0 == completion.DeviceIndex)
                                    await connection.FailNotificationAsync(completion.NotificationId, completion.BatchId, CancellationToken.None).ConfigureAwait(false);
                                else if (completion.DeviceIndex == completion.Devices.Count)
                                    await connection.CompleteNotificationAsync(completion.NotificationId, completion.BatchId, CancellationToken.None).ConfigureAwait(false);
                                else
                                {
                                    await connection.CompletePartialNotificationAsync(completion.NotificationId, completion.BatchId,
                                        completion.Devices.Take(completion.DeviceIndex).Cast<SqlApnsDevice>().Select(d => d.DeviceId),
                                        CancellationToken.None).ConfigureAwait(false);
                                }
                            }

                            return;
                        }
                        catch (SqlException ex)
                        {
                            // TODO: Only retry on those exceptions where it make sense to do so...
                            Debug.WriteLine("Update failed: " + ex.Message);
                        }

                        if (null == _rng)
                            _rng = new XorShift1024Star();

                        delay *= 1.3;

                        await _rng.RandomDelay(TimeSpan.FromMilliseconds(delay), TimeSpan.FromMilliseconds(delay * 1.5), CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
