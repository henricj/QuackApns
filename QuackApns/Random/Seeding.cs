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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace QuackApns.Random
{
    public static class Seeding
    {
        public static void Seed(byte[] input, uint[] output)
        {
            if (null == input)
                throw new ArgumentNullException("input");
            if (null == output)
                throw new ArgumentNullException("output");
            if (input.Length != output.Length * sizeof(uint))
                throw new ArgumentOutOfRangeException("input", "input/output size mismatch");

            Buffer.BlockCopy(input, 0, output, 0, input.Length);
        }

        public static void Seed(byte[] input, ulong[] output)
        {
            if (null == input)
                throw new ArgumentNullException("input");
            if (null == output)
                throw new ArgumentNullException("output");
            if (input.Length != output.Length * sizeof(ulong))
                throw new ArgumentOutOfRangeException("input", "input/output size mismatch");

            Buffer.BlockCopy(input, 0, output, 0, input.Length);
        }

        public static void Seed(ulong[] output)
        {
            if (null == output)
                throw new ArgumentNullException("output");

            var seed = Seed(output.Length * sizeof(ulong));

            Seed(seed, output);
        }

        public static void Seed(uint[] output)
        {
            if (null == output)
                throw new ArgumentNullException("output");

            var seed = Seed(output.Length * sizeof(uint));

            Seed(seed, output);
        }

        public static byte[] Seed(int length)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException("length", "must be positive");

            var seed = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seed);
            }

            return seed;
        }

        public static void Seed(int key, ulong[] output)
        {
            if (null == output)
                throw new ArgumentNullException("output");

            var bytes = Seed(sizeof(ulong) * output.Length, key).ToArray();

            Seed(bytes, output);
        }

        public static void Seed(int key, uint[] output)
        {
            if (null == output)
                throw new ArgumentNullException("output");

            var bytes = Seed(sizeof(uint) * output.Length, key).ToArray();

            Seed(bytes, output);
        }

        static IEnumerable<byte> Seed(int length, int key)
        {
            byte[] hash = null;

            using (var hmac = new HMACSHA512(BitConverter.GetBytes(key)))
            {
                while (length > 0)
                {
                    hmac.Initialize();

                    if (null != hash)
                        hmac.TransformBlock(hash, 0, hash.Length, null, 0);

                    var lengthBytes = BitConverter.GetBytes(length);

                    hmac.TransformFinalBlock(lengthBytes, 0, lengthBytes.Length);

                    hash = hmac.Hash;

                    foreach (var b in hash)
                    {
                        if (0 >= length)
                            yield break;

                        yield return b;

                        --length;
                    }
                }
            }
        }
    }
}
