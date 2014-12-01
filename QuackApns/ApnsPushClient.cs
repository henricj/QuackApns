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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace QuackApns
{
    public class ApnsPushClient
    {
        readonly BufferBlock<IReadOnlyCollection<ApnsNotification>> _bufferBlock = new BufferBlock<IReadOnlyCollection<ApnsNotification>>(new DataflowBlockOptions { BoundedCapacity = 50 });
        readonly string _host;
        readonly Instance[] _instances;
        readonly int _port;

        public ApnsPushClient(string host, int port, int parallelConnections = 4)
        {
            if (null == host)
                throw new ArgumentNullException("host");

            _host = host;
            _port = port;
            _instances = new Instance[parallelConnections];
        }

        public bool Post(IReadOnlyCollection<ApnsNotification> notifications)
        {
            return _bufferBlock.Post(notifications);
        }

        public Task<bool> SendAsync(IReadOnlyCollection<ApnsNotification> notifications, CancellationToken cancellationToken)
        {
            return _bufferBlock.SendAsync(notifications, cancellationToken);
        }

        public void Push()
        {
            _bufferBlock.Post(null);
        }

        public void Shutdown()
        {
            _bufferBlock.Complete();
        }

        #region Nested type: Instance

        class Instance
        {
            readonly ApnsPushConnection _connection;
            readonly int _count;
            readonly int _id;
            Task _task;

            public Instance(int id, int count)
            {
                if (id >= count || id < 0)
                    throw new ArgumentOutOfRangeException("id", "invalid id: " + id);
                if (count < 1)
                    throw new ArgumentOutOfRangeException("count", "invalid count: " + count);

                _id = id;
                _count = count;
                _connection = new ApnsPushConnection();
            }

            public ApnsPushConnection Connection
            {
                get { return _connection; }
            }

            bool Filter(ApnsDevice device)
            {
                return _id == (device.TokenChecksum % _count);
            }
        }

        #endregion
    }
}
