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
using System.Threading;
using System.Threading.Tasks;
using QuackApns.Utility;

namespace PushSharpClient
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

        static Task RunAsync(string host, int port, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 1; ++i)
            {
                var push = new PushSharpApnsClient();

                var writerTask = push.PushSharpAsync(host, port, cancellationToken);

                tasks.Add(writerTask);
            }

            return Task.WhenAll(tasks);
        }
    }
}
