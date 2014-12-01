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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuackApns.Utility;

namespace QuackApns.Test
{
    [TestClass]
    public class FletcherTests
    {
        [TestMethod]
        public void ZeroLength()
        {
            var zero = new byte[0];

            var sum = Fletcher.Fletcher16(zero, 0, 0);

            var simpleSum = SimpleFletcher16(zero, 0);

            Assert.AreEqual(simpleSum, sum);
        }

        [TestMethod]
        public void KnownAnswer()
        {
            var buffer = new byte[] { 1, 2 };
            const ushort checksum = 0x0403;

            var sum = Fletcher.Fletcher16(buffer, 0, buffer.Length);

            Assert.AreEqual(checksum, sum);
        }

        [TestMethod]
        public void KnownAnswerWithOffset()
        {
            var buffer = new byte[] { 0, 1, 2 };
            const ushort checksum = 0x0403;

            var sum = Fletcher.Fletcher16(buffer, 1, buffer.Length - 1);

            Assert.AreEqual(checksum, sum);
        }

        [TestMethod]
        public void OneByte()
        {
            var buffer = new byte[1];

            for (short i = byte.MinValue; i <= byte.MaxValue; ++i)
            {
                buffer[0] = (byte)i;

                var sum = Fletcher.Fletcher16(buffer, 0, buffer.Length);

                var simpleSum = SimpleFletcher16(buffer, buffer.Length);

                Assert.AreEqual(simpleSum, sum);
            }
        }

        [TestMethod]
        public void AllZeros1k()
        {
            var buffer = Enumerable.Repeat((byte)0, 1024).ToArray();

            for (var i = 2; i <= buffer.Length; ++i)
            {
                var sum = Fletcher.Fletcher16(buffer, 0, i);

                var simpleSum = SimpleFletcher16(buffer, i);

                Assert.AreEqual(simpleSum, sum);
            }
        }

        [TestMethod]
        public void AllOnes1k()
        {
            var buffer = Enumerable.Repeat((byte)1, 1024).ToArray();

            for (var i = 2; i <= buffer.Length; ++i)
            {
                var sum = Fletcher.Fletcher16(buffer, 0, i);

                var simpleSum = SimpleFletcher16(buffer, i);

                Assert.AreEqual(simpleSum, sum);
            }
        }

        [TestMethod]
        public void AllFs1k()
        {
            var buffer = Enumerable.Repeat((byte)0xff, 1024).ToArray();

            for (var i = 2; i <= buffer.Length; ++i)
            {
                var sum = Fletcher.Fletcher16(buffer, 0, i);

                var simpleSum = SimpleFletcher16(buffer, i);

                Assert.AreEqual(simpleSum, sum);
            }
        }

        ushort SimpleFletcher16(byte[] buffer, int count)
        {
            byte sum1 = 0;
            byte sum2 = 0;

            for (var i = 0; i < count; ++i)
            {
                sum1 = checked((byte)((sum1 + buffer[i]) % 255));
                sum2 = checked((byte)((sum2 + sum1) % 255));
            }

            if (0 == sum1)
                sum1 = 0xff;
            if (0 == sum2)
                sum2 = 0xff;

            return checked((ushort)((sum2 << 8) | sum1));
        }
    }
}
