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
using System.Threading;
using System.Threading.Tasks;
using QuackApns.Network;
using QuackApns.Parser;

namespace QuackApns
{
    public class ApnsReader : INetConnectionHandler
    {
        // https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/CommunicatingWIthAPS.html

        readonly TaskCompletionSource<object> _doneReading = new TaskCompletionSource<object>();

        readonly ApnsNotification _notification = new ApnsNotification();
        readonly IParser[] _parsers = { new Type0Parse(), new Type1Parser(), new Type2Parser() };
        IParser _parser;

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var total = 0L;

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

                        var tmp = readBuffer;
                        readBuffer = parseBuffer;
                        parseBuffer = tmp;
                    }

                    readTask = stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

                    if (length > 0)
                        Parse(parseBuffer, 0, length);
                }

                return total;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Read failed: " + ex.Message);

                return total;
            }
            finally
            {
                _doneReading.TrySetResult(null);
            }
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;

                tcs.TrySetCanceled();
            }, _doneReading))
            {
                await _doneReading.Task.ConfigureAwait(false);
            }

            return 0;
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
                        // TODO: Fail the stream...
                        return;
                    }


                    _parser = _parsers[type];

                    _notification.Type = type;

                    _parser.Start(_notification);

                    if (i >= count)
                        return;
                }

                i += _parser.Parse(buffer, offset + i, count - i);

                if (_parser.IsDone)
                {
                    // TODO: Do something with the _notification.
                    _parser = null;
                }
            }
        }

        uint ReadBigEndianUint(Stream stream)
        {
            return (uint)
                ((stream.ReadByte() << 24)
                 | (stream.ReadByte() << 16)
                 | (stream.ReadByte() << 8)
                 | stream.ReadByte());
        }

        ushort ReadBigEndianUshort(Stream stream)
        {
            return (ushort)
                ((stream.ReadByte() << 8)
                 | stream.ReadByte());
        }

        ushort ReadItemHeader(Stream stream, out ApnsItemId itemId)
        {
            itemId = (ApnsItemId)stream.ReadByte();

            return (ushort)((stream.ReadByte() << 8) | stream.ReadByte());
        }
    }
}
