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

namespace QuackApns
{
    public static class ApnsStreamExtensions
    {
        public static void WriteBigEndian(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        public static void WriteBigEndian(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        public static void WriteBlock(Stream stream, ApnsItemId itemId, byte[] buffer, int offset, int count)
        {
            if (count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("count", "buffer is too long");
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset", "invalid offset: " + offset);
            if (count < 1 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count", "invalid count: " + count);

            stream.WriteByte((byte)itemId);

            WriteBigEndian(stream, (ushort)count);

            stream.Write(buffer, offset, count);
        }

        public static void WriteItemHeader(Stream stream, ApnsItemId itemId, ushort length)
        {
            stream.WriteByte((byte)itemId);
            stream.WriteByte((byte)(length >> 8));
            stream.WriteByte((byte)length);
        }

        public static uint ReadBigEndianUint(Stream stream)
        {
            return (uint)
                ((stream.ReadByte() << 24)
                 | (stream.ReadByte() << 16)
                 | (stream.ReadByte() << 8)
                 | stream.ReadByte());
        }

        public static ushort ReadBigEndianUshort(Stream stream)
        {
            return (ushort)
                ((stream.ReadByte() << 8)
                 | stream.ReadByte());
        }

        public static ushort ReadItemHeader(Stream stream, out ApnsItemId itemId)
        {
            itemId = (ApnsItemId)stream.ReadByte();

            return (ushort)((stream.ReadByte() << 8) | stream.ReadByte());
        }
    }
}
