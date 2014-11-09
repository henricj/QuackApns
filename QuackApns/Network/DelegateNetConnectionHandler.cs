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
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.Network
{
    public class DelegateNetConnectionHandler : INetConnectionHandler
    {
        static readonly Task<long> ZeroResult = Task.FromResult(0L);

        public Func<Stream, CancellationToken, Task<long>> Write { get; set; }

        public Func<Stream, CancellationToken, Task<long>> Read { get; set; }

        public Func<CancellationToken, Task> Close { get; set; }

        #region INetConnectionHandler Members

        public Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var readAsync = Read;

            if (null == readAsync)
                return ZeroResult;

            return readAsync(stream, cancellationToken);
        }

        public Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            var writeAsync = Write;

            if (null == writeAsync)
                return ZeroResult;

            return writeAsync(stream, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            var closeAsync = Close;

            if (null == closeAsync)
                return ZeroResult;

            return closeAsync(cancellationToken);
        }

        #endregion
    }
}
