using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
    public class RawTcpClient
    {
        static readonly byte[] BigBuffer = new byte[128 * 1024];

        public static async Task WriterAsync(string host, int port, CancellationToken cancellationToken)
        {
            using (var tcpClient = new TcpClient())
            {
                tcpClient.SendBufferSize = 256 * 1024;

                using (cancellationToken.Register(o =>
                {
                    var client = (TcpClient)o;

                    try
                    {
                        client.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("TcpClient close on cancel failed: " + ex.Message);
                    }
                }, tcpClient))
                {
                    await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

                    using (var s = tcpClient.GetStream())
                    {
                        for (var i = 0; i < 500; ++i)
                            await s.WriteAsync(BigBuffer, 0, BigBuffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}