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
using System.Threading;
using System.Threading.Tasks;
using QuackApns;
using QuackApns.Certificates;
using QuackApns.Network;
using QuackApns.Utility;

namespace TestServer
{
    class Program
    {
        const string ServerP12File = "server.p12";

        static async Task RunAsync(string hostname, int port, CancellationToken cancellationToken)
        {
            var pushServer = new NetServer(ct => Task.FromResult<INetConnectionHandler>(new ApnsNotificationReader()));

            var feedbackServer = new NetServer(ct => Task.FromResult<INetConnectionHandler>(new DelegateNetConnectionHandler()));

            var certificate = await IsolatedStorageCertificates.GetCertificateAsync(hostname, ServerP12File, false, cancellationToken).ConfigureAwait(false);

            await pushServer.StartAsync(hostname, port, certificate, cancellationToken).ConfigureAwait(false);

            await feedbackServer.StartAsync(hostname, port + 1, certificate, cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(pushServer.WaitAsync(), feedbackServer.WaitAsync()).ConfigureAwait(false);

            await pushServer.CloseAsync().ConfigureAwait(false);

            await feedbackServer.CloseAsync().ConfigureAwait(false);
        }

        static void Main(string[] args)
        {
            try
            {
                ConsoleCancel.RunAsync(ct => RunAsync("localhost", 12321, ct), TimeSpan.Zero).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(args[0] + " failed: " + ex.Message);
            }
        }
    }
}
