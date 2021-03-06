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
    public static class DateTimeExtensions
    {
        static readonly DateTimeOffset EpochBase = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static long ToUnixEpoch(this DateTimeOffset time)
        {
            return (long)Math.Round((time - EpochBase).TotalSeconds);
        }

        public static int ToInt32UnixEpoch(this DateTimeOffset time)
        {
            return (int)Math.Round((time - EpochBase).TotalSeconds);
        }

        public static long ToUnixEpoch(this DateTime time)
        {
            return (long)Math.Round((time.ToUniversalTime() - EpochBase).TotalSeconds);
        }

        public static int ToInt32UnixEpoch(this DateTime time)
        {
            return (int)Math.Round((time.ToUniversalTime() - EpochBase).TotalSeconds);
        }
    }
}
