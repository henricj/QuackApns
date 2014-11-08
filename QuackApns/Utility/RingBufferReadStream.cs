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

namespace QuackApns.Utility
{
    public class RingBufferReadStream : Stream
    {
        readonly byte[] _buffer;
        int _count;
        int _readIndex;

        public RingBufferReadStream(int bufferSize)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", "size must be positive");

            _buffer = new byte[bufferSize];
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _count; }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public bool IsFull
        {
            get { return _count == _buffer.Length; }
        }

        public override void Flush()
        { }

        public override int ReadByte()
        {
            if (0 == _count)
                return -1;

            var b = _buffer[_readIndex];

            --_count;

            if (++_readIndex == _buffer.Length)
                _readIndex = 0;

            return b;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (null == buffer)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count + offset > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            var totalCopy = 0;

            if (0 == _count || 0 == count)
                return 0;

            if (_readIndex + _count > _buffer.Length)
            {
                if (_readIndex < _buffer.Length)
                {
                    var copy = Math.Min(count, _buffer.Length - _readIndex);

                    Array.Copy(_buffer, _readIndex, buffer, offset, copy);

                    _readIndex += copy;
                    _count -= copy;

                    if (0 == count)
                        return copy;

                    if (0 == _count)
                    {
                        _readIndex = 0;
                        return copy;
                    }

                    totalCopy += copy;

                    offset += copy;
                    count -= copy;
                }

                Debug.Assert(_readIndex == _buffer.Length, "We are at the end of the buffer");

                _readIndex = 0;
            }

            Debug.Assert(_count > 0, "We can't be empty here");
            Debug.Assert(_readIndex + _count <= _buffer.Length, "We shouldn't wrap around here");

            {
                var copy = Math.Min(count, _count);

                Array.Copy(_buffer, _readIndex, buffer, offset, copy);

                _readIndex += copy;
                _count -= copy;

                if (0 == _count)
                    _readIndex = 0;

                totalCopy += copy;

                return totalCopy;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (SeekOrigin.Current != origin)
                throw new NotSupportedException();

            if (offset <= 0)
                return 0;

            if (offset >= _count)
            {
                _count = 0;
                _readIndex = 0;

                return 0;
            }

            _count -= (int)offset;

            return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public ArraySegment<byte> GetFreeSpace()
        {
            if (IsFull)
                return new ArraySegment<byte>();

            var endIndex = _readIndex + _count;

            if (endIndex < _buffer.Length)
                return new ArraySegment<byte>(_buffer, endIndex, _buffer.Length - endIndex);

            endIndex -= _buffer.Length;

            return new ArraySegment<byte>(_buffer, endIndex, _readIndex - endIndex);
        }

        public void AddBytes(int count)
        {
            if (count <= 0 || _count + count > _buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            _count += count;
        }
    }
}
