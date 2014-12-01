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
using Newtonsoft.Json;

namespace QuackApns
{
    public sealed class ApnsJsonWriter : IDisposable
    {
        readonly ApnsNotificationWriter _apnsWriter;
        readonly MemoryStream _jsonStream = new MemoryStream(256);
        readonly JsonWriter _jsonWriter;

        public ApnsJsonWriter(ApnsNotificationWriter apnsWriter = null)
        {
            _apnsWriter = apnsWriter ?? new ApnsNotificationWriter();
            _jsonWriter = new JsonTextWriter(new StreamWriter(_jsonStream));
        }

        #region IDisposable Members

        public void Dispose()
        {
            _jsonWriter.Close();
        }

        #endregion

        public void Write(Stream stream, uint identifier, int expirationEpoch, byte[] deviceId, IApnsPayloadWriter payload)
        {
            _jsonStream.SetLength(0);

            payload.Write(_jsonWriter);

            _jsonWriter.Flush();

            _apnsWriter.Write(stream, identifier, expirationEpoch, 10, deviceId, _jsonStream.GetBuffer(), 0, (int) _jsonStream.Length);
        }
    }
}
