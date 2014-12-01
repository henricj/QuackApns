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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.RedisRepository
{
    class RegistrationFeedback : IRegistrationFeedback
    {
        readonly ICollection<RedisConnection.RegistrationEvent> _events;

        public RegistrationFeedback(ICollection<RedisConnection.RegistrationEvent> events)
        {
            if (null == events)
                throw new ArgumentNullException("events");

            _events = events;
        }

        internal ICollection<RedisConnection.RegistrationEvent> Events
        {
            get { return _events; }
        }

        #region IRegistrationFeedback Members

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (null == _events)
                return 0;

            var length = (4 + 2 + 32) * _events.Count;

            var keyBuffer = new byte[ApnsConstants.DeviceTokenLength];

            using (var ms = new MemoryStream(length))
            {
                foreach (var e in _events)
                {
                    ApnsStreamExtensions.WriteBigEndian(ms, (uint)e.UnixTimestamp);
                    ApnsStreamExtensions.WriteBigEndian(ms, 32);

                    e.GetDeviceKey(keyBuffer, 0, keyBuffer.Length);

                    ms.Write(keyBuffer, 0, keyBuffer.Length);
                }

                await stream.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length, cancellationToken).ConfigureAwait(false);
            }

            return length;
        }

        #endregion
    }
}
