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
using QuackApns;
using QuackApns.Data;
using QuackApns.Network;
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

            for (var i = 0; i < 2; ++i)
            {
                var writerTask = RunWriterAsync(host, port, cancellationToken);

                tasks.Add(writerTask);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        static Task RunWriterAsync(string host, int port, CancellationToken cancellationToken)
        {
            var pushClient = new ApnsPushClient();

            var sw = Stopwatch.StartNew();

            var tcpConnection = new NetConnection(pushClient);

            var writerTask = tcpConnection.RunAsync(host, port, true, cancellationToken);

            var genTask = Task.Run(() =>
            {
                for (var j = 0; j < 10; ++j)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var notifications = CreateNotifications(1000 * 1000);

                    pushClient.Post(notifications);
                }

                var flushTask = pushClient.FlushAsync(true, cancellationToken)
                    .ContinueWith(t =>
                    {
                        sw.Stop();

                        var messages = pushClient.MessageCount;
                        var written = pushClient.BytesWritten;
                        var elapsed = sw.Elapsed;

                        if (elapsed > TimeSpan.Zero && messages > 0)
                        {
                            Console.WriteLine("Wrote {0:N0} kMsg totaling {1:F2}MB in {2} at {3:F2}MB/s", messages / 1000.0, written / (1024.0 * 1024.0), elapsed, written / (elapsed.TotalSeconds * 1024.0 * 1024.0));
                            Console.WriteLine("Wrote {0:F2} kMsg/s averaging {1} bytes/Msg", messages / elapsed.TotalMilliseconds, written / (double)messages);
                        }

                        Console.WriteLine("Done " + t.Result);
                    },
                        TaskContinuationOptions.ExecuteSynchronously);
            }, cancellationToken);

            return writerTask;
        }

        static ICollection<ApnsNotification> CreateNotifications(int count)
        {
            var generator = new ApnsNotificationGenerator();

            var payload = new ApnsPayload { Alert = "Hello " + DateTimeOffset.Now, Badge = 1, Sound = "default" };

            var sw = Stopwatch.StartNew();

            var notifications = generator.GetApnsNotifications(count, TimeSpan.FromDays(1), 10, payload);
            //var notifications = generator.GetApnsNotifications(count, TimeSpan.FromDays(1), 10);

            sw.Stop();

            var elapsed = sw.Elapsed;

            Console.WriteLine("Created {0:N0} kMsg in {1} ({2:F2}kMsg/s)", count / 1000.0, elapsed, count / elapsed.TotalMilliseconds);

            return notifications;
        }
    }
}
