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

namespace QuackApns.Utility
{
    public static class Fletcher
    {
        public static ushort Fletcher16(byte[] buffer, int offset, int count)
        {
            ushort sum1 = 0xff;
            ushort sum2 = 0xff;

            for (var i = 0; i < count; )
            {
                for (var batch = count - i >= 20 ? 20 : count - i; batch > 0; --batch)
                {
                    sum1 = (ushort)(sum1 + buffer[offset + i]);
                    sum2 = (ushort)(sum2 + sum1);

                    ++i;
                }

                sum1 = (ushort)((sum1 & 0xff) + (sum1 >> 8));
                sum2 = (ushort)((sum2 & 0xff) + (sum2 >> 8));
            }

            sum1 = (ushort)((sum1 & 0xff) + (sum1 >> 8));
            sum2 = (ushort)((sum2 & 0xff) + (sum2 >> 8));

            return (ushort)((sum2 << 8) | sum1);
        }
    }
}
