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
using QuackApns.Random;
using QuackApns.Utility;

namespace QuackApns.Data
{
    public class ApnsNotificationGenerator
    {
        readonly XorShift1024Star _rng;

        public ApnsNotificationGenerator()
        {
            _rng = new XorShift1024Star();
        }

        public ApnsNotificationGenerator(int key)
        {
            _rng = new XorShift1024Star(key);
        }

        public ICollection<ApnsNotification> GetApnsNotifications(int count, TimeSpan relativeExpiration, byte priority, IApnsPayloadWriter payload)
        {
            var binaryMessage = payload.ToBinary();

            var now = DateTimeOffset.UtcNow;

            var expiration = now + relativeExpiration;
            var expirationEpoch = expiration.ToInt32UnixEpoch();

            var notifications = new ApnsNotification[count];

            for (var i = 0; i < count; ++i)
            {
                var apnsNotification = new ApnsNotification
                {
                    Device = new byte[ApnsConstants.DeviceTokenLength],
                    ExpirationEpoch = expirationEpoch,
                    Payload = new ArraySegment<byte>(binaryMessage),
                    Priority = priority
                };

                _rng.GetBytes(apnsNotification.Device);

                notifications[i] = apnsNotification;
            }

            return notifications;
        }

        public ICollection<ApnsNotification> GetApnsNotifications(int count, TimeSpan relativeExpiration, byte priority)
        {
            var now = DateTimeOffset.UtcNow;

            var expiration = now + relativeExpiration;
            var expirationEpoch = expiration.ToInt32UnixEpoch();

            var notifications = new ApnsNotification[count];

            for (var i = 0; i < count; ++i)
            {
                var payload = new ApnsPayload { Alert = "Testing " + _rng.NextDouble(), Badge = 1, Sound = "default" };

                var binaryMessage = payload.ToBinary();

                var apnsNotification = new ApnsNotification
                {
                    Device = new byte[ApnsConstants.DeviceTokenLength],
                    ExpirationEpoch = expirationEpoch,
                    Payload = new ArraySegment<byte>(binaryMessage),
                    Priority = priority
                };

                _rng.GetBytes(apnsNotification.Device);

                notifications[i] = apnsNotification;
            }

            return notifications;
        }
    }
}
