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
using QuackApns.Network;
using QuackApns.Utility;

namespace QuackApns
{
    public class ApnsFeedbackWriter : INetConnectionHandler
    {
        readonly TaskCompletionSource<object> _doneWriting = new TaskCompletionSource<object>();
        readonly IFeedbackSource _feedbackSource;
        IRegistrationFeedback _feedback;
        bool _readOk;
        bool _writeOk;

        public ApnsFeedbackWriter(IFeedbackSource feedbackSource)
        {
            if (null == feedbackSource)
                throw new ArgumentNullException("feedbackSource");

            _feedbackSource = feedbackSource;
        }

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[256];
            var total = 0L;

            using (cancellationToken.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;

                tcs.TrySetCanceled();
            }, _doneWriting))
            {
                for (; ; )
                {
                    var length = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                    if (length < 1)
                    {
                        _readOk = true;
                        return total;
                    }

                    total += length;
                }
            }
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            _feedback = await _feedbackSource.GetFeedbackAsync(cancellationToken).ConfigureAwait(false);

            if (null == _feedback)
                return 0;
            
            var length = await _feedback.WriteAsync(stream, cancellationToken).ConfigureAwait(false);

            _writeOk = true;

            return length;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            var feedback = _feedback;
            _feedback = null;

            if (null == feedback || !_writeOk || !_readOk)
                return TplHelpers.CompletedTask;

            return _feedbackSource.CompleteAsync(feedback, cancellationToken);
        }

        #endregion
    }
}
