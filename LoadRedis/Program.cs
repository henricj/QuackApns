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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuackApns.RedisRepository;
using QuackApns.Utility;

namespace LoadRedis
{
    class Program
    {
        static async Task LoadAsync(int key, int count, CancellationToken cancellationToken)
        {
            try
            {
                using (var connection = await RedisConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
                {
                    await connection.LoadAsync(key, count, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load {0} of {1:N0} failed: {2}", key, count, ex.Message);
            }
        }

        static async Task RunAsync(CancellationToken cancellationToken)
        {
            await LoadRegistrationsAsync(cancellationToken);
        }

        static async Task LoadRegistrationsAsync(CancellationToken cancellationToken)
        {
            using (var connection = await RedisConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                var registrations = await connection.GetPendingRegistrationsAsync(cancellationToken).ConfigureAwait(false);

                if (null == registrations)
                    return;

                //await connection.ClearPendingRegistrationsAsync(registrations, cancellationToken).ConfigureAwait(false);

                Console.WriteLine("Registrations: {0:N0}", registrations.Count);
            }
        }

        static Task LoadDeregistrationsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
