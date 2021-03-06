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
using System.Linq;

namespace QuackApns.Parser
{
    abstract class ParserBase : IParser
    {
        protected ApnsNotification Notification { get; private set; }
        protected ApnsDevice Device { get; private set; }

        protected Action<ApnsResponse> ReportError { get; private set; }

        #region IParser Members

        public bool IsDone { get; protected set; }

        public virtual void Start(ApnsNotification notification, Action<ApnsResponse> reportError)
        {
            if (null == notification)
                throw new ArgumentNullException("notification");
            if (null == reportError)
                throw new ArgumentNullException("reportError");

            Notification = notification;
            ReportError = reportError;
            IsDone = false;

            if (null == Notification.Devices)
            {
                Device = new ApnsDevice(new byte[ApnsConstants.DeviceTokenLength]);

                var devices = new[] { Device };

                Notification.Devices = devices;
            }

            Device = Notification.Devices.FirstOrDefault();
        }

        public abstract int Parse(byte[] buffer, int offset, int count);

        #endregion
    }
}
