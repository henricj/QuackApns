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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.Network
{
    public sealed class NetServer : IDisposable
    {
        readonly Func<CancellationToken, Task<INetConnectionHandler>> _connectionHandlerFactory;
        Task[] _listeners;
        TaskCompletionSource<object> _serverDone = new TaskCompletionSource<object>();

        public NetServer(Func<CancellationToken, Task<INetConnectionHandler>> connectionHandlerFactory)
        {
            if (null == connectionHandlerFactory)
                throw new ArgumentNullException("connectionHandlerFactory");

            _connectionHandlerFactory = connectionHandlerFactory;
        }

        #region IDisposable Members

        public void Dispose()
        { }

        #endregion

        public async Task StartAsync(string hostname, int port, X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            if (null != _listeners)
                throw new InvalidOperationException("The server is in use");

            if (_serverDone.Task.IsCompleted)
                _serverDone = new TaskCompletionSource<object>();

            var serverDone = _serverDone;

            var ips = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);

            if (0 == ips.Length)
            {
                Console.WriteLine("Nothing to listen for on {0}:{1}", hostname, port);
                _serverDone.TrySetResult(null);

                return;
            }

            _listeners = ips
                .Select(ip => Task.Run(() => ListenAsync(ip, port, certificate, cancellationToken), cancellationToken))
                .ToArray();

            var complete = Task.WhenAll(_listeners).ContinueWith(t => serverDone.TrySetResult(null));

            Console.WriteLine("Listening on {0}:{1}", hostname, port);
        }

        public async Task CloseAsync()
        {
            if (null == _listeners)
                return;

            await Task.WhenAll(_listeners).ConfigureAwait(false);

            _listeners = null;
        }

        public Task WaitAsync()
        {
            return _serverDone.Task;
        }

        async Task ListenAsync(IPAddress ip, int port, X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            var listener = new TcpListener(ip, port);
            var handlers = new HashSet<Task>();
            var handlersDone = new TaskCompletionSource<object>();

            try
            {
                var server = listener.Server;

                server.LingerState = new LingerOption(true, 10);
                server.NoDelay = true;
                server.ReceiveBufferSize = 1024 * 1024;

                listener.Start();

                using (cancellationToken.Register(x =>
                {
                    var l = ((TcpListener)x);

                    try
                    {
                        l.Stop();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Listener cancellation: " + ex.Message);
                    }
                }, listener))
                {
                    for (; ; )
                    {
                        var connectionHandler = await _connectionHandlerFactory(cancellationToken).ConfigureAwait(false);

                        var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                        var handlerTask = Task.Run(() => RunClientHandlerAsync(tcpClient, connectionHandler, certificate, cancellationToken), cancellationToken);

                        lock (handlers)
                        {
                            handlers.Add(handlerTask);

                            if (handlersDone.Task.IsCompleted)
                                handlersDone = new TaskCompletionSource<object>();
                        }

                        var cleanup = handlerTask.ContinueWith(t =>
                        {
                            TaskCompletionSource<object> tcs = null;

                            bool removed;
                            lock (handlers)
                            {
                                removed = handlers.Remove(handlerTask);

                                if (0 == handlers.Count)
                                    tcs = handlersDone;
                            }

                            if (!removed)
                                Debug.WriteLine("Server remove handler failed");

                            if (null != tcs)
                                tcs.TrySetResult(null);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
            }
            catch (OperationCanceledException)
            { }
            catch (ObjectDisposedException ex)
            {
                if (ex.ObjectName != typeof(Socket).FullName)
                    Debug.WriteLine("ReaderAsync failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReaderAsync failed: " + ex.Message);
            }
            finally
            {
                listener.Stop();
            }


            // Should we let TPL handle the child Tasks?
            try
            {
                Task[] handlersCopy;
                lock (handlers)
                {
                    handlersCopy = handlers.ToArray();
                }

                await Task.WhenAll(handlersCopy).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReaderAsync waiting for readers to finish: " + ex.Message);
            }

            lock (handlers)
            {
                Debug.Assert(0 == handlers.Count, "All readers should have completed");
            }
        }

        async Task RunClientHandlerAsync(TcpClient tcpClient, INetConnectionHandler connectionHandler, X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            var connection = tcpClient.Client.LocalEndPoint + " <--> " + tcpClient.Client.RemoteEndPoint;

            Console.WriteLine("Accepted connection " + connection);

            var total = 0L;

            using (cancellationToken.Register(o =>
            {
                var socket = (Socket)o;

                try
                {
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TcpClient shutdown on cancel failed: " + ex.Message);
                }
            }, tcpClient.Client, false))
            {
                try
                {
                    using (var stream = tcpClient.GetStream())
                    {
                        var s = stream as Stream;

                        if (null != certificate)
                            s = await ConnectTlsAsync(stream, certificate, cancellationToken).ConfigureAwait(false);

                        var reader = ReaderAsync(connectionHandler, tcpClient.Client, s, cancellationToken);

                        var writer = WriterAsync(connectionHandler, tcpClient.Client, s, cancellationToken);

                        await Task.WhenAll(reader, writer).ConfigureAwait(false);

                        total += await reader.ConfigureAwait(false);

                        total += await writer.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                { }
                catch (Exception ex)
                {
                    Debug.WriteLine("ClientHandler failed: " + ex.Message);
                }
            }

            try
            {
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Reader cancel close failed: " + ex.Message);
            }

            var totalMb = total * (1.0 / (1024.0 * 1024.0));

            Console.WriteLine("Closed connection " + connection + " after " + totalMb.ToString("F2") + " MB");
        }

        static async Task<Stream> ConnectTlsAsync(Stream stream, X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            var ssl = new SslStream(stream, false, ValidateRemoteCertificate); //, null, EncryptionPolicy.RequireEncryption);

            await ssl.AuthenticateAsServerAsync(certificate, true, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false).ConfigureAwait(false);

            return ssl;
        }

        static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static async Task<long> ReaderAsync(INetConnectionHandler connectionHandler, Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                var length = await connectionHandler.ReadAsync(stream, cancellationToken).ConfigureAwait(false);

                return length;
            }
            finally
            {
                try
                {
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Receive);
                }
                catch (Exception)
                {
                    // Best effort
                }
            }
        }

        async Task<long> WriterAsync(INetConnectionHandler connectionHandler, Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                var length = await connectionHandler.WriteAsync(stream, cancellationToken).ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                return length;
            }
            finally
            {
                try
                {
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception)
                {
                    // Best effort
                }
            }
        }
    }
}
