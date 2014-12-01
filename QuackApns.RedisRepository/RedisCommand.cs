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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace QuackApns.RedisRepository
{
    public abstract class RedisCommand : TaskCompletionSource<object>
    {
        public virtual int ByteLength { get; protected set; }
        protected IRedisMessageParser Parser { get; set; }

        public abstract void WriteCommand(Stream stream);

        public virtual void Start(RedisParserContext context, byte messageType)
        {
            Parser = context.GetParser(messageType);
        }

        public virtual bool ParseResponse(byte[] buffer, int offset, int count, out int used)
        {
            Parser = Parser.Parser(buffer, offset, count, out used);

            return null == Parser;
        }
    }

    abstract class RedisMessage
    { }

    class RedisInteger : RedisMessage
    {
        readonly long _value;

        public RedisInteger(long value)
        {
            _value = value;
        }

        public long Value
        {
            get { return _value; }
        }
    }

    class RedisArray
    {
        IReadOnlyCollection<RedisMessage> _value;

        public IReadOnlyCollection<RedisMessage> Value
        {
            get { return _value; }
        }
    }

    public sealed class PingCommand : RedisCommand
    {
        static readonly byte[] PingBytes = RedisDefaultSerializer.Command.Serialize("PING");
        Stopwatch _timer;

        public TimeSpan Elapsed { get { return _timer.Elapsed; } }

        public override int ByteLength
        {
            get { return PingBytes.Length; }
        }

        public override void WriteCommand(Stream stream)
        {
            _timer = Stopwatch.StartNew();

            stream.Write(PingBytes, 0, PingBytes.Length);
        }

        public override bool ParseResponse(byte[] buffer, int offset, int count, out int used)
        {
            var done = base.ParseResponse(buffer, offset, count, out used);

            if (done)
                _timer.Stop();

            return done;
        }
    }

    public sealed class QuitCommand : RedisCommand
    {
        static readonly byte[] QuitBytes = RedisDefaultSerializer.Command.Serialize("QUIT");

        public override int ByteLength
        {
            get { return QuitBytes.Length; }
        }

        public override void WriteCommand(Stream stream)
        {
            stream.Write(QuitBytes, 0, QuitBytes.Length);
        }
    }
}
