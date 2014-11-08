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

using Newtonsoft.Json;

namespace QuackApns
{
    public sealed class ApnsPayload : IApnsPayloadWriter
    {
        public string Sound { get; set; }
        public int? Badge { get; set; }
        public string Alert { get; set; }

        #region IApnsPayloadSerializer Members

        public void Write(JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("aps");

            writer.WriteStartObject(); // aps

            if (!string.IsNullOrWhiteSpace(Alert))
            {
                writer.WritePropertyName("alert");
                writer.WriteValue(Alert.Trim());
            }

            if (Badge.HasValue)
            {
                writer.WritePropertyName("badge");
                writer.WriteValue(Badge.Value);
            }

            if (!string.IsNullOrWhiteSpace(Sound))
            {
                writer.WritePropertyName("sound");
                writer.WriteValue(Sound.Trim());
            }

            writer.WriteEndObject(); // aps

            writer.WriteEndObject();
        }

        #endregion
    }
}
