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
using System.IO;

namespace QuackApns.Parser
{
    class Type2Parser : ParserBase
    {
        MemoryStream _frameBuffer;
        int _frameSize;
        int _index;

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
                if (_index < 3)
                {
                    _frameSize <<= 8;
                    _frameSize |= buffer[offset + i];

                    ++i;
                    ++_index;
                }
                else if (_index < 4)
                {
                    _frameSize <<= 8;
                    _frameSize |= buffer[offset + i];

                    ++i;
                    ++_index;

                    // Got frame size...
                    if (null == _frameBuffer)
                        _frameBuffer = new MemoryStream(_frameSize);
                    else
                    {
                        if (_frameBuffer.Capacity < _frameSize)
                            _frameBuffer.Capacity = _frameSize;

                        _frameBuffer.SetLength(0);
                    }
                }
                else
                {
                    var remaining = (int)(_frameSize - _frameBuffer.Length);

                    var copy = Math.Min(remaining, count - i);

                    _frameBuffer.Write(buffer, offset + i, copy);

                    i += copy;
                    _index += copy;

                    if (_frameBuffer.Length == _frameSize)
                    {
                        ParseFrame();

                        IsDone = true;

                        break;
                    }
                }
            }

            return i;
        }

        void ParseFrame()
        { }
    }
}
