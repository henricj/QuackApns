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
using System.Linq;

namespace QuackApns.Parser
{
    class Type0Parser : ParserBase
    {
        int _index;
        ushort _payloadLength;
        ushort _tokenLength;

        public override void Start(ApnsNotification notification, Action<ApnsResponse> reportError)
        {
            base.Start(notification, reportError);

            _index = 0;
        }

        public override int Parse(byte[] buffer, int offset, int count)
        {
            if (IsDone)
                return 0;

            var i = 0;

            while (i < count)
            {
                if (_index < 1)
                {
                    _tokenLength = (ushort)(buffer[offset + i] << 8);

                    ++i;
                    ++_index;
                }
                else if (_index < 2)
                {
                    _tokenLength |= buffer[offset + i];

                    ++i;
                    ++_index;

                    if (_tokenLength != ApnsConstants.DeviceTokenLength)
                        ReportError(null);

                    if (_tokenLength != Device.Token.Length)
                        ReportError(null);
                }
                else if (_index < 2 + _tokenLength)
                {
                    var deviceIndex = _index - (2 + _tokenLength);

                    var remaining = _tokenLength - deviceIndex;

                    var copy = Math.Min(remaining, count - i);

                    if (copy > 1)
                        Array.Copy(buffer, offset + i, Device.Token, deviceIndex, copy);
                    else
                        Device.Token[deviceIndex] = buffer[offset + i];

                    _index += copy;
                    i += copy;
                }
                else if (_index < 2 + _tokenLength + 1)
                {
                    _payloadLength = (ushort)(buffer[offset + i] << 8);

                    ++i;
                    ++_index;
                }
                else if (_index < 2 + _tokenLength + 2)
                {
                    _payloadLength |= buffer[offset + i];

                    if (_payloadLength > 256)
                        ReportError(null);

                    if (null == Notification.Payload.Array || _payloadLength != Notification.Payload.Count)
                        Notification.Payload = new ArraySegment<byte>(new byte[_payloadLength]);

                    ++i;
                    ++_index;

                }
                else
                {
                    var payloadIndex = _index - (2 + _tokenLength + 2);

                    var remaining = _payloadLength - payloadIndex;

                    var copy = Math.Min(remaining, count - i);

                    if (copy > 1)
                        Array.Copy(buffer, offset + i, Notification.Payload.Array, payloadIndex, copy);
                    else
                        Notification.Payload.Array[payloadIndex] = buffer[offset + i];

                    _index += copy;
                    i += copy;

                    if (copy == remaining)
                    {
                        IsDone = true;

                        break;
                    }
                }
            }

            return i;
        }
    }
}
