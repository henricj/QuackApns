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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace QuackApns.Network
{
    public class NetConnection
    {
        readonly INetConnectionHandler _connectionHandler;

        public NetConnection(INetConnectionHandler connectionHandler)
        {
            if (null == connectionHandler)
                throw new ArgumentNullException("connectionHandler");

            _connectionHandler = connectionHandler;
        }

        public X509Certificate2Collection ClientCertificates { get; set; }

        public async Task RunAsync(string host, int port, bool useTls, CancellationToken cancellationToken)
        {
            using (var tcpClient = new TcpClient())
            {
                tcpClient.LingerState = new LingerOption(true, 10);
                tcpClient.NoDelay = true;
                tcpClient.SendBufferSize = 1024 * 1024;

                using (cancellationToken.Register(o =>
                {
                    var client = (TcpClient)o;

                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("TcpClient close on cancel failed: " + ex.Message);
                    }
                }, tcpClient, false))
                {
                    await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

                    using (var stream = tcpClient.GetStream())
                    {
                        var s = stream as Stream;

                        if (useTls)
                            s = await ConnectTlsAsync(stream, host, cancellationToken).ConfigureAwait(false);

                        var readerTask = ReaderAsync(tcpClient.Client, s, cancellationToken);

                        var writerTask = WriterAsync(tcpClient.Client, s, cancellationToken);

                        await Task.WhenAll(readerTask, writerTask).ConfigureAwait(false);
                    }
                }
            }
        }

        async Task<Stream> ConnectTlsAsync(Stream stream, string host, CancellationToken cancellationToken)
        {
            var bufferedStream = stream; //new BufferedStream(stream, 512 * 1024);

            var ssl = new SslStream(bufferedStream, false, ValidateRemoteCertificate, SelectLocalCertificate, EncryptionPolicy.RequireEncryption);

            await ssl.AuthenticateAsClientAsync(host, ClientCertificates, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false).ConfigureAwait(false);

            return ssl;
        }

        X509Certificate SelectLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return null;
        }

        bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        async Task<long> ReaderAsync(Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                return await _connectionHandler.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Receive);
                }
                catch (Exception)
                {
                    // Best effort...
                }
            }
        }

        async Task<long> WriterAsync(Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                var length = await _connectionHandler.WriteAsync(stream, cancellationToken).ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                return length;
            }
            finally
            {
                try
                {
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
