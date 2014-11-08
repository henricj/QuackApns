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

namespace QuackApns.Parser
{
    class Type1Parser : IParser
    {
        bool _done;
        uint _expiration;
        uint _identifier;
        int _index;
        ApnsNotification _notification;
        ushort _payloadLength;
        ushort _tokenLength;

        #region IParser Members

        public bool IsDone
        {
            get { return _done; }
        }

        public void Start(ApnsNotification notification)
        {
            _notification = notification;
            _index = 0;
            _done = false;
        }

        public int Parse(byte[] buffer, int offset, int count)
        {
            if (_done)
                return 0;

            var i = 0;

            while (i < count)
            {
                if (_index < 4)
                {
                    _identifier <<= 8;
                    _identifier |= buffer[offset + i];

                    ++i;
                    ++_index;
                }
                else if (_index < 8)
                {
                    _expiration <<= 8;
                    _expiration |= buffer[offset + i];

                    ++i;
                    ++_index;
                }
                else if (_index < 8 + 1)
                {
                    _tokenLength = (ushort)(buffer[offset + i] << 8);

                    ++i;
                    ++_index;
                }
                else if (_index < 8 + 2)
                {
                    _tokenLength |= buffer[offset + i];

                    ++i;
                    ++_index;

                    if (null == _notification.Device || _tokenLength != _notification.Device.Length)
                        _notification.Device = new byte[_tokenLength];
                }
                else if (_index < 8 + 2 + _tokenLength)
                {
                    var deviceIndex = _index - (8 + 2);

                    var remaining = _tokenLength - deviceIndex;

                    var copy = Math.Min(remaining, count - i);

                    if (copy > 1)
                        Array.Copy(buffer, offset + i, _notification.Device, deviceIndex, copy);
                    else
                        _notification.Device[deviceIndex] = buffer[offset + i];

                    _index += copy;
                    i += copy;
                }
                else if (_index < 8 + 2 + _tokenLength + 1)
                {
                    _payloadLength = (ushort)(buffer[offset + i] << 8);

                    ++i;
                    ++_index;
                }
                else if (_index < 8 + 2 + _tokenLength + 2)
                {
                    _payloadLength |= buffer[offset + i];

                    if (null == _notification.Payload.Array || _payloadLength != _notification.Payload.Count)
                        _notification.Payload = new ArraySegment<byte>(new byte[_payloadLength]);

                    ++i;
                    ++_index;
                }
                else if (_index < 8 + 2 + _tokenLength + 2 + _payloadLength)
                {
                    var payloadIndex = _index - (8 + 2 + _tokenLength + 2);

                    var remaining = _tokenLength - payloadIndex;

                    var copy = Math.Min(remaining, count - i);

                    if (copy > 1)
                        Array.Copy(buffer, offset + i, _notification.Payload.Array, payloadIndex, copy);
                    else
                        _notification.Payload.Array[payloadIndex] = buffer[offset + i];

                    _index += copy;
                    i += copy;

                    if (copy == remaining)
                    {
                        _done = true;

                        break;
                    }
                }
            }

            return i;
        }

        #endregion
    }
}
