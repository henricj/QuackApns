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

namespace QuackApns.Random
{
    public class XorShift1024Star : IRandomGenerator<ulong>
    {
        // http://arxiv.org/abs/1402.6246
        const double DoubleScale = (1.0 / ulong.MaxValue);
        readonly ulong[] _s = new ulong[16];
        int _p;

        public XorShift1024Star()
        {
            Reseed();
        }

        public XorShift1024Star(int seed)
        {
            Reseed(seed);
        }

        #region IRandomGenerator<ulong> Members

        public void Reseed()
        {
            _p = 0;

            Seeding.Seed(_s);
        }


        public void Reseed(int seed)
        {
            _p = 0;

            Seeding.Seed(seed, _s);
        }

        public ulong Next()
        {
            var s0 = _s[_p];
            var s1 = _s[_p = (_p + 1) & 15];

            s1 ^= s1 << 31; // a
            s1 ^= s1 >> 11; // b
            s0 ^= s0 >> 30; // c

            return (_s[_p] = s0 ^ s1) * 1181783497276652981L;
        }

        #endregion

        public void GetBytes(byte[] buffer, int offset, int count)
        {
            for (; ; )
            {
                var v = Next();

                for (var j = 0; j < sizeof(ulong); ++j)
                {
                    if (count <= 0)
                        return;

                    buffer[offset++] = (byte)v;
                    --count;
                    v >>= 8;
                }
            }
        }

        public double NextDouble()
        {
            return Next() * DoubleScale;
        }
    }
}
