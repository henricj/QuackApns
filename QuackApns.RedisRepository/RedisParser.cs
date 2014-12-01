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

using System.Diagnostics;
using System.IO;

namespace QuackApns.RedisRepository
{
    public interface IRedisMessageParser
    {
        void Start(byte messageType);
        IRedisMessageParser Parser(byte[] buffer, int offset, int count, out int used);
    }

    abstract class RedisMessageParser : IRedisMessageParser
    {
        byte _messageType;

        public byte MessageType
        {
            get { return _messageType; }
        }

        #region IRedisMessageParser Members

        public virtual void Start(byte messageType)
        {
            _messageType = messageType;
        }

        public abstract IRedisMessageParser Parser(byte[] buffer, int offset, int count, out int used);

        #endregion
    }

    sealed class RedisDiscardParser : RedisMessageParser
    {
        bool _haveCr;

        public override void Start(byte messageType)
        {
            _haveCr = false;
        }

        public override IRedisMessageParser Parser(byte[] buffer, int offset, int count, out int used)
        {
            for (var i = 0; i < count; ++i)
            {
                var b = buffer[offset + i];

                if (_haveCr && '\n' == b)
                {
                    used = i;

                    return null;
                }

                _haveCr = '\r' == b;
            }

            used = count;

            return this;
        }
    }

    class RedisLineParser : RedisMessageParser
    {
        readonly MemoryStream _memoryStream = new MemoryStream();

        bool _haveCr;

        public override void Start(byte messageType)
        {
            base.Start(messageType);

            _memoryStream.SetLength(0);

            _haveCr = false;
        }

        public override IRedisMessageParser Parser(byte[] buffer, int offset, int count, out int used)
        {
            for (var i = 0; i < count; ++i)
            {
                var b = buffer[offset + i];

                if (_haveCr && '\n' == b)
                {
                    if (i > 1)
                        _memoryStream.Write(buffer, offset, i - 1);

                    used = i + 1;

                    return null;
                }

                _haveCr = '\r' == b;
            }

            if (!_haveCr)
                _memoryStream.Write(buffer, offset, count);
            else
            {
                if (count > 1)
                    _memoryStream.Write(buffer, offset, count - 1);
            }

            used = count;

            return this;
        }
    }

    class RedisIntegerParser : RedisMessageParser
    {
        bool _haveCr;
        long _value;

        public override void Start(byte messageType)
        {
            base.Start(messageType);

            _haveCr = false;
            _value = 0;
        }

        public override IRedisMessageParser Parser(byte[] buffer, int offset, int count, out int used)
        {
            for (var i = 0; i < count; ++i)
            {
                var b = buffer[offset + i];

                if (_haveCr && '\n' == b)
                {
                    used = i;

                    return null;
                }

                _haveCr = '\r' == b;

                if (b >= '0' && b <= '9')
                {
                    _value *= 10;
                    _value += b - '0';
                }
            }

            used = count;

            return this;
        }
    }

    class RedisParser
    {
        readonly RedisParserContext _redisParserContext = new RedisParserContext();
        RedisCommand _command;

        bool _commandStarted;

        public bool Parse(byte[] buffer, int offset, int count, out int used)
        {
            for (var i = 0; i < count; )
            {
                if (!_commandStarted)
                {
                    var messageType = buffer[offset + i];

                    ++i;

                    _command.Start(_redisParserContext, messageType);

                    _commandStarted = true;

                    continue;
                }

                int parserUsed;
                var done = _command.ParseResponse(buffer, offset + i, count - i, out parserUsed);

                Debug.Assert(parserUsed >= 0 && parserUsed <= count, "Used out of range");

                i += parserUsed;

                if (done)
                {
                    // We are done with the command.
                    _commandStarted = false;

                    used = i;

                    return true;
                }
            }

            used = count;

            return false;
        }

        public void Flush()
        { }

        public void SetCommand(RedisCommand command)
        {
            _command = command;
            _commandStarted = false;
        }
    }
}
