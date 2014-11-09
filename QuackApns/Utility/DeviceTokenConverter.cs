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

namespace QuackApns.Utility
{
    public class DeviceTokenConverter
    {
        readonly char[] _chars = new char[64];

        char ToHexChar(int b)
        {
            return (char)(b + (b < 10 ? '0' : 'a' - 10));
        }

        byte ToByte(char hex)
        {
            if (hex >= '0' && hex <= '9')
                return (byte)(hex - '0');
            if (hex >= 'a' && hex <= 'f')
                return (byte)(hex - ('a' - 10));
            if (hex >= 'A' && hex <= 'F')
                return (byte)(hex - ('A' - 10));

            throw new FormatException("Invalid hex character: " + (int)hex);
        }

        byte ToByte(char hi, char lo)
        {
            return (byte)((ToByte(hi) << 4) | ToByte(lo));
        }

        public string TokenToString(byte[] token)
        {
            if (null == token)
                throw new ArgumentNullException("token");
            if (token.Length != ApnsConstants.DeviceTokenLength)
                throw new ArgumentException("token", "tokens must be " + ApnsConstants.DeviceTokenLength + " bytes" + token.Length);

            for (var i = 0; i < ApnsConstants.DeviceTokenLength; ++i)
            {
                var b = token[i];

                _chars[2 * i] = ToHexChar(b >> 4);
                _chars[2 * i + 1] = ToHexChar(b & 0x0f);
            }

            return new string(_chars);
        }

        public void StringToToken(string token, byte[] buffer, int offset, int count)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException("token");
            if (null == buffer)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException("offset", offset, "outside the buffer");
            if (ApnsConstants.DeviceTokenLength != count)
                throw new ArgumentOutOfRangeException("count", count, "tokens are  " + ApnsConstants.DeviceTokenLength + "  bytes");
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count", count, "outside the buffer");

            token = token.Trim();

            if (64 != token.Length)
                throw new ArgumentException("tokens must be " + ApnsConstants.DeviceTokenLength + " bytes ( " + 2 * ApnsConstants.DeviceTokenLength + "  chars): " + token.Length, "token");

            for (var i = 0; i < ApnsConstants.DeviceTokenLength; ++i)
            {
                var hi = token[2 * i];
                var lo = token[2 * i + 1];

                buffer[offset + i] = ToByte(hi, lo);
            }
        }
    }

    public static class DeviceTokenConverterExtensions
    {
        public static void StringToToken(this DeviceTokenConverter deviceTokenConverter, string token, byte[] buffer)
        {
            deviceTokenConverter.StringToToken(token, buffer, 0, buffer.Length);
        }

        public static byte[] StringToToken(this DeviceTokenConverter deviceTokenConverter, string token)
        {
            var buffer = new byte[ApnsConstants.DeviceTokenLength];

            deviceTokenConverter.StringToToken(token, buffer);

            return buffer;
        }
    }
}
