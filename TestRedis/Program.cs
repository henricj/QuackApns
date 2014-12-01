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
using System.Threading;
using System.Threading.Tasks;
using QuackApns.Network;
using QuackApns.RedisRepository;
using QuackApns.Utility;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: host port");
                return;
            }

            var host = args[0];

            int port;
            if (!int.TryParse(args[1], out port))
            {
                Console.WriteLine("Bad port: " + args[1]);
                Console.WriteLine("Usage: host port");
                return;
            }

            try
            {
                ConsoleCancel.RunAsync(ct => RunAsync(host, port, ct), TimeSpan.FromMinutes(5)).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(args[0] + " failed: " + ex.Message);
            }
        }

        static async Task RunAsync(string host, int port, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 1; ++i)
            {
                var writerTask = RunWriterAsync(host, port, cancellationToken);

                tasks.Add(writerTask);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        static Task RunWriterAsync(string host, int port, CancellationToken cancellationToken)
        {
            var redisClient = new RedisClient();

            var sw = Stopwatch.StartNew();

            var tcpConnection = new NetConnection(redisClient);

            var writerTask = tcpConnection.RunAsync(host, port, false, cancellationToken);

            var cleanupTask = writerTask.ContinueWith(async t =>
            {
                try
                {
                    await redisClient.CloseAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RedisClient close failed: " + ex.Message);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            var genTask = Task.Run(async () =>
            {
                var commands = new PingCommand[10];

                for (var repeat = 0; repeat < 10; ++repeat)
                {
                    for (var j = 0; j < commands.Length; ++j)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var pingCommand = new PingCommand();

                        //var pingSw = new Stopwatch();

                        //pingCommand.Task.ContinueWith(t => pingSw.Stop(), TaskContinuationOptions.ExecuteSynchronously)
                        //pingCommand.Task.ContinueWith(t => Console.WriteLine("PONG: " + run + " " + t.Status + " in " + pingCommand.Elapsed));

                        //pingSw.Start();

                        redisClient.Post(pingCommand);

                        commands[j] = pingCommand;
                    }

                    redisClient.Push();

                    await commands[commands.Length - 1].Task.ConfigureAwait(false);

                    foreach (var command in commands)
                    {
                        Console.WriteLine("PONG: " + repeat + " " + command.Task.Status + " in " + command.Elapsed);
                    }
                }
            }, cancellationToken);

            return writerTask;
        }
    }
}
