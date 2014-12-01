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
using System.Threading.Tasks.Dataflow;
using QuackApns;
using QuackApns.Network;
using QuackApns.SqlServerRepository;
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

        static async Task RunWriterAsync(string host, int port, CancellationToken cancellationToken)
        {
            using (var sqlNotificationResultHandler = new SqlNotificationWriter())
            {
                var sw = new Stopwatch();

                var pushClient = new ApnsPushConnection();

                using (pushClient.LinkTo(sqlNotificationResultHandler.TargetBlock, new DataflowLinkOptions { PropagateCompletion = true }))
                {
                    var tcpConnection = new NetConnection(pushClient);

                    var writerTask = tcpConnection.RunAsync(host, port, true, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    sw.Start();

                    await LoadNotificationsAsync(pushClient, cancellationToken).ConfigureAwait(false);

                    Debug.WriteLine("Calling pushClient.Complete()");

                    pushClient.Complete();

                    Debug.WriteLine("Waiting for writerTask");

                    await writerTask.ConfigureAwait(false);

                    Debug.WriteLine("Calling pushClient.CloseAsync()");

                    await pushClient.CloseAsync(cancellationToken).ConfigureAwait(false);

                    Debug.WriteLine("Waiting for sql writer to complete");

                    await sqlNotificationResultHandler.Completion.ConfigureAwait(false);

                    sw.Stop();
                }

                var messages = pushClient.NotificationCount;
                var written = pushClient.BytesWritten;
                var elapsed = sw.Elapsed;

                if (elapsed > TimeSpan.Zero && messages > 0)
                {
                    Console.WriteLine("Wrote {0:N0} Msg totaling {1:F2}MB in {2} at {3:F2}MB/s", messages, written / (1024.0 * 1024.0), elapsed, written / (elapsed.TotalSeconds * 1024.0 * 1024.0));
                    Console.WriteLine("Wrote {0:F2} kMsg/s averaging {1:F2} bytes/Msg", messages / elapsed.TotalMilliseconds, written / (double)messages);
                }
            }
        }

        static async Task LoadNotificationsAsync(ApnsPushConnection push, CancellationToken cancellationToken)
        {
            try
            {
                using (var connection = await SqlServerConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
                {
                    for (; ; )
                    {
                        var notifications = await connection.GetPendingNotificationsAsync(cancellationToken).ConfigureAwait(false);

                        if (null == notifications || 0 == notifications.Count)
                            break;

                        foreach (var notification in notifications)
                        {
                            await push.SendAsync(notification, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadNotificationsAsync failed: " + ex.Message);
            }
        }
    }
}
