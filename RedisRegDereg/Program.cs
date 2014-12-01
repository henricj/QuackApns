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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QuackApns.Random;
using QuackApns.RedisRepository;
using QuackApns.Utility;

namespace LoadRedis
{
    class Program
    {
        static async Task PoissonEventsAsync(IRandomGenerator generator, double rate, Func<int, CancellationToken, Task<int>> eventHandler, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            var eventTime = TimeSpan.Zero;
            var eventsRequested = 0;
            var eventsHandled = 0;

            for (; ; )
            {
                while (eventTime < sw.Elapsed)
                {
                    var nextEvent = generator.NextExponential(rate);

                    eventTime += TimeSpan.FromTicks((int)Math.Round(nextEvent * TimeSpan.TicksPerSecond));

                    ++eventsRequested;
                }

                if (eventsRequested > eventsHandled)
                {
                    var count = eventsRequested - eventsHandled;

                    var actual = await eventHandler(count, cancellationToken).ConfigureAwait(false);

                    Debug.WriteLine("Handled events: " + actual);

                    eventsHandled += count;
                    //eventsHandled += actual;
                }

                var delay = eventTime - sw.Elapsed;

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        static Task DeregAsync(RedisConnection connection, IRandomGenerator generator, double rate, CancellationToken cancellationToken)
        {
            return PoissonEventsAsync(generator, rate, (count, ct) => connection.DeregisterRandomDevicesAsync(generator, count, ct), cancellationToken);
        }

        static Task RegAsync(RedisConnection connection, IRandomGenerator generator, double rate, CancellationToken cancellationToken)
        {
            return PoissonEventsAsync(generator, rate, (count, ct) => connection.RegisterRandomDevicesAsync(generator, count, ct), cancellationToken);
        }

        static async Task RunAsync(CancellationToken cancellationToken)
        {
            var regRatePerSec = 0.75;
            var deregRatePerSec = 0.75;

            using (var connection = await RedisConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                var regTask = RegAsync(connection, new XorShift1024Star(), regRatePerSec, cancellationToken);

                var deregTask = DeregAsync(connection, new XorShift1024Star(), deregRatePerSec, cancellationToken);

                await Task.WhenAll(regTask, deregTask).ConfigureAwait(false);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                ConsoleCancel.RunAsync(RunAsync, TimeSpan.Zero).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.Flatten().InnerExceptions)
                {
                    if (!(inner is OperationCanceledException))
                        Console.WriteLine("failed: " + inner.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed: " + ex.Message);
            }
        }
    }
}
