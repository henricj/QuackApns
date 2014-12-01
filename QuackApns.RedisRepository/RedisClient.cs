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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using QuackApns.Network;

namespace QuackApns.RedisRepository
{
    public class RedisClient : INetConnectionHandler
    {
        readonly RedisParser _parser = new RedisParser();
        readonly BufferBlock<RedisCommand> _readBlock = new BufferBlock<RedisCommand>();
        readonly byte[] _readBuffer = new byte[64 * 1024];
        readonly BufferBlock<RedisCommand> _writeBlock = new BufferBlock<RedisCommand>();
        readonly BufferedWriter _writer = new BufferedWriter(32 * 1024);
        int _readCount;
        int _readOffset;

        #region INetConnectionHandler Members

        public async Task<long> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var readerBlock = new ActionBlock<RedisCommand>(command => ReadCommandAsync(command, stream, cancellationToken),
                new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken, BoundedCapacity = 1 });

            using (_readBlock.LinkTo(readerBlock, new DataflowLinkOptions { PropagateCompletion = true }))
            {
                await readerBlock.Completion.ConfigureAwait(false);
            }

            CancelReadBuffer();

            return 0;
        }

        public async Task<long> WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            var writerBlock = new ActionBlock<RedisCommand>(command => WriteCommandAsync(command, stream, cancellationToken),
                new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken, BoundedCapacity = 1 });

            using (_writeBlock.LinkTo(writerBlock, new DataflowLinkOptions { PropagateCompletion = true }))
            {
                await writerBlock.Completion.ConfigureAwait(false);
            }

            await WriteCommandAsync(new QuitCommand(), stream, cancellationToken).ConfigureAwait(false);

            await _writer.FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

            await _writer.WaitAsync(stream, cancellationToken).ConfigureAwait(false);

            return _writer.BytesWritten;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _writeBlock.Complete();

            _readBlock.Complete();

            CancelReadBuffer();

            CancelWriteBuffer();

            return Task.WhenAll(_writeBlock.Completion, _readBlock.Completion);
        }

        #endregion

        void CancelReadBuffer()
        {
            RedisCommand command;
            while (_readBlock.TryReceive(out command))
            {
                if (null != command)
                    command.TrySetCanceled();
            }
        }

        void CancelWriteBuffer()
        {
            RedisCommand command;
            while (_writeBlock.TryReceive(out command))
            {
                if (null != command)
                    command.TrySetCanceled();
            }
        }

        async Task ReadCommandAsync(RedisCommand command, Stream stream, CancellationToken cancellationToken)
        {
            _parser.SetCommand(command);

            for (; ; )
            {
                if (0 == _readCount)
                {
                    CompactBuffer();

                    var length = await stream.ReadAsync(_readBuffer, _readOffset, _readBuffer.Length - _readOffset, cancellationToken).ConfigureAwait(false);

                    if (length < 1)
                    {
                        command.TrySetCanceled();
                        return;
                    }

                    _readCount += length;
                }

                int used;
                var done = _parser.Parse(_readBuffer, _readOffset, _readCount, out used);

                if (used > 0)
                {
                    _readOffset += used;
                    _readCount -= used;
                }
                else
                {
                    command.TrySetCanceled();

                    return;
                }

                if (done)
                {
                    command.TrySetResult(null);

                    return;
                }
            }
        }

        void CompactBuffer()
        {
            if (0 == _readOffset)
                return;

            if (0 == _readCount)
            {
                _readOffset = 0;

                return;
            }

            if (_readBuffer.Length - (_readOffset + _readCount) > 256)
                return;

            Array.Copy(_readBuffer, _readOffset, _readBuffer, 0, _readCount);

            _readOffset = 0;
        }

        async Task WriteCommandAsync(RedisCommand command, Stream stream, CancellationToken cancellationToken)
        {
            if (null == command)
            {
                _writer.TryFlushBuffer(stream, cancellationToken);

                return;
            }

            if (!await _readBlock.SendAsync(command, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Unable to send read command");

            var length = command.ByteLength;

            if (length > _writer.BytesRemaining)
                await _writer.FlushBufferAsync(stream, cancellationToken).ConfigureAwait(false);

            command.WriteCommand(_writer.BufferStream);

            if (0 == _writeBlock.Count)
                _writer.TryFlushBuffer(stream, cancellationToken);
        }

        public bool Post(RedisCommand command)
        {
            return _writeBlock.Post(command);
        }

        public Task SendAsync(RedisCommand command, CancellationToken cancellationToken)
        {
            return _writeBlock.SendAsync(command, cancellationToken);
        }

        public bool Push()
        {
            return _writeBlock.Post(null);
        }
    }
}
