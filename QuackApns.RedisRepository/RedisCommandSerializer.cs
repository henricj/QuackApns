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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuackApns.Utility;

namespace QuackApns.RedisRepository
{
    public interface IRedisCommandSerializer
    {
        void Serialize(Stream stream, IReadOnlyCollection<object> values);
        byte[] Serialize(string command);
    }

    public class RedisCommandSerializer : IRedisCommandSerializer
    {
        static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false);
        readonly Encoding _encoding;
        readonly MemoryStream _memoryStream = new MemoryStream();
        readonly TextWriter _streamWriter;

        public RedisCommandSerializer(Encoding encoding = null)
        {
            _encoding = encoding ?? Utf8Encoding;
            _streamWriter = CreateWriter(_memoryStream);
        }

        #region IRedisCommandSerializer Members

        public void Serialize(Stream stream, IReadOnlyCollection<object> values)
        {
            using (var sw = CreateWriter(stream))
            {
                sw.WriteLine("*" + values.Count.ToString(CultureInfo.InvariantCulture));

                foreach (var value in values)
                {
                    var bytes = value as IEnumerable<byte>;

                    if (null != bytes)
                    {
                        var array = bytes as byte[] ?? bytes.ToArray();

                        WriteBytes(stream, sw, array, 0, array.Length);
                    }
                    else
                    {
                        _streamWriter.WriteLine("{0}", value);

                        _streamWriter.Flush();

                        WriteBytes(stream, sw, _memoryStream.GetBuffer(), 0, (int)_memoryStream.Length);

                        _memoryStream.SetLength(0);

                        sw.WriteLine();

                        sw.Flush();
                    }
                }
            }
        }

        public byte[] Serialize(string command)
        {
            _streamWriter.WriteLine("*1");

            _streamWriter.WriteLine("$" + _encoding.GetByteCount(command).ToString(CultureInfo.InvariantCulture));

            _streamWriter.WriteLine(command);

            _streamWriter.Flush();

            var bytes = _memoryStream.ToArray();

            _memoryStream.SetLength(0);

            return bytes;
        }

        #endregion

        static void WriteBytes(Stream stream, TextWriter textWriter, byte[] data, int offset, int count)
        {
            textWriter.WriteLine("$" + data.Length.ToString(CultureInfo.InvariantCulture));

            textWriter.Flush();

            stream.Write(data, offset, count);
        }

        TextWriter CreateWriter(Stream stream)
        {
            return new FormatStreamWriter(stream, _encoding, 1024, true, CultureInfo.InvariantCulture) { NewLine = "\r\n" };
        }
    }
}
