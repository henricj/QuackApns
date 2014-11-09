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

namespace QuackApns
{
    public interface IApnsNotificationWriter
    {
        void Write(Stream stream, int identifier, int expirationEpoch, byte priority, byte[] deviceId, byte[] payload, int payloadOffset, int payloadCount);
    }

    public sealed class ApnsNotificationWriter : IApnsNotificationWriter
    {
        // https://developer.apple.com/library/ios/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/Chapters/CommunicatingWIthAPS.html

        public void Write(Stream stream, int identifier, int expirationEpoch, byte priority, byte[] deviceId, byte[] payload, int payloadOffset, int payloadCount)
        {
            // iOS 8 supports 2k?
            if (payloadCount > 2048) //if (message.Length > 256)
                throw new ArgumentOutOfRangeException("payload", "message is too big");

            var frameLength = 3 + deviceId.Length // device token
                              + 3 + payloadCount // payload
                              + 3 + 4 // identifier
                              + 3 + 4 // expiration
                              + 3 + 1; // priority

            stream.WriteByte(2);
            ApnsStreamExtensions.WriteBigEndian(stream, (uint)frameLength);

            ApnsStreamExtensions.WriteBlock(stream, ApnsItemId.DeviceToken, deviceId, 0, deviceId.Length);

            ApnsStreamExtensions.WriteBlock(stream, ApnsItemId.Payload, payload, payloadOffset, payloadCount);

            ApnsStreamExtensions.WriteItemHeader(stream, ApnsItemId.Identifier, 4);
            ApnsStreamExtensions.WriteBigEndian(stream, (uint)identifier);

            ApnsStreamExtensions.WriteItemHeader(stream, ApnsItemId.Expiration, 4);
            ApnsStreamExtensions.WriteBigEndian(stream, (uint)expirationEpoch);

            ApnsStreamExtensions.WriteItemHeader(stream, ApnsItemId.Priority, 1);
            stream.WriteByte(priority);
        }
    }

    public static class ApnsNotificationWriterExtensions
    {
        public static void Write(this IApnsNotificationWriter writer, Stream stream, ApnsNotification notification)
        {
            var payload = notification.Payload;

            writer.Write(stream, notification.Identifier, notification.ExpirationEpoch, notification.Priority, notification.Device, payload.Array, payload.Offset, payload.Count);
        }
    }
}
