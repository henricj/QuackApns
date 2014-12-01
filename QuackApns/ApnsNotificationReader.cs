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
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuackApns.Network;
using QuackApns.Parser;
using QuackApns.Utility;

namespace QuackApns
{
    public class ApnsNotificationReader : INetConnectionHandler
    {
        // https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/CommunicatingWIthAPS.html

        readonly TaskCompletionSource<ApnsResponse> _doneReading = new TaskCompletionSource<ApnsResponse>();

        readonly ApnsNotification _notification = new ApnsNotification();
        readonly IParser[] _parsers = { new Type0Parser(), new Type1Parser(), new Type2Parser() };
        long _messageCount;
        IParser _parser;
        long _readBytes;
        long _totalTicks;
        uint _lastGoodIdentifier;

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var total = 0L;

            var sw = Stopwatch.StartNew();

            try
            {
                var readBuffer = new byte[16 * 1024];
                var parseBuffer = new byte[16 * 1024];
                Task<int> readTask = null;

                var maxLength = 0;

                for (; ; )
                {
                    var length = 0;

                    if (null != readTask)
                    {
                        length = await readTask.ConfigureAwait(false);

                        if (length < 1)
                            break;

                        if (length > maxLength)
                            maxLength = length;

                        total += length;
                        Interlocked.Add(ref _readBytes, length);

                        var tmp = readBuffer;
                        readBuffer = parseBuffer;
                        parseBuffer = tmp;
                    }

                    readTask = stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

                    if (length > 0)
                        Parse(parseBuffer, 0, length);
                }
            }
            catch (IOException ex)
            {
                var socketException = ex.InnerException as SocketException;

                if (null == socketException || SocketError.ConnectionReset != socketException.SocketErrorCode)
                    Debug.WriteLine("Read failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Read failed: " + ex.Message);
            }
            finally
            {
                sw.Stop();

                Interlocked.Add(ref _totalTicks, sw.ElapsedTicks);

                _doneReading.TrySetResult(null);
            }

            return total;
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            ApnsResponse response;

            using (cancellationToken.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;

                tcs.TrySetCanceled();
            }, _doneReading))
            {
                response = await _doneReading.Task.ConfigureAwait(false);
            }

            if (null == response)
                return 0;

            using (var ms = new MemoryStream())
            {
                var w = new ApnsResponseWriter();

                w.Write(ms, response);

                var length = (int)ms.Length;

                try
                {
                    await stream.WriteAsync(ms.GetBuffer(), 0, length, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    var socketException = ex.InnerException as SocketException;

                    if (null == socketException || SocketError.ConnectionReset != socketException.SocketErrorCode)
                        Debug.WriteLine("Write failed: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Write failed: " + ex.Message);
                }

                return length;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            var messages = Interlocked.Read(ref _messageCount);
            var read = Interlocked.Read(ref _readBytes);
            var elapsed = TimeSpan.FromTicks((long)Math.Round((1e7) * Interlocked.Read(ref _totalTicks) / Stopwatch.Frequency));

            var sb = new StringBuilder();

            sb.AppendFormat("Read {0:N0} Msg totaling {1:F2}MB in {2} at {3:F2}MB/s", messages, read / (1024.0 * 1024.0), elapsed, read / (elapsed.TotalSeconds * 1024.0 * 1024.0));
            sb.AppendLine();
            sb.AppendFormat("     {0:F2} kMsg/s averaging {1:F2} bytes/Msg", messages / elapsed.TotalMilliseconds, read / (double)messages);

            Console.WriteLine(sb.ToString());

            return TplHelpers.CompletedTask;
        }

        #endregion

        void Parse(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; )
            {
                if (null == _parser)
                {
                    var type = buffer[offset + i];

                    ++i;

                    if (type >= _parsers.Length)
                    {
                        _doneReading.TrySetResult(new ApnsResponse { ErrorCode = ApnsErrorCode.ProcessingError });
                        return;
                    }

                    _parser = _parsers[type];

                    _notification.Type = type;

                    _parser.Start(_notification, response => _doneReading.TrySetResult(response));

                    if (i >= count)
                        return;
                }

                i += _parser.Parse(buffer, offset + i, count - i);

                if (_parser.IsDone)
                {
                    Interlocked.Increment(ref _messageCount);

                    // TODO: Do something with the _notification.

                    if (!_doneReading.Task.IsCompleted)
                        _lastGoodIdentifier = _notification.Devices[0].Identifier;
                    _parser = null;
                }
            }
        }
    }
}
