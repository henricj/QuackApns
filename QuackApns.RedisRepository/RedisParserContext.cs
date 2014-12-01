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

namespace QuackApns.RedisRepository
{
    public class RedisParserContext
    {
        readonly IRedisMessageParser _discardParser = new RedisDiscardParser();
        readonly IRedisMessageParser _integerParser = new RedisIntegerParser();
        readonly IRedisMessageParser _lineParser = new RedisLineParser();

        public IRedisMessageParser GetParser(byte messageType)
        {
            IRedisMessageParser parser;

            switch ((char)messageType)
            {
                case '+': // Simple string
                case '-': // Error
                    parser = _lineParser;
                    break;
                case ':': // Integer
                case '$': // Bulk String
                case '*': // Array
                    parser = _integerParser;
                    break;
                default:
                    parser = _discardParser;
                    break;
            }

            parser.Start(messageType);

            return parser;
        }
    }
}
