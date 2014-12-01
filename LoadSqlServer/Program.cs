using System.Threading;
using QuackApns.SqlServerRepository;
using QuackApns.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadSqlServer
{
    class Program
    {
        static async Task LoadAsync(int key, int count, CancellationToken cancellationToken)
        {
            try
            {
                using (var connection = await SqlServerConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
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
            var tasks = Enumerable.Range(1, 1).Select(i => Task.Run(() => LoadAsync(i, 1000 * 1000, cancellationToken), cancellationToken)).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            using (var connection = await SqlServerConnection.ConnectAsync(cancellationToken).ConfigureAwait(false))
            {

                var count = await connection.GetCountAsync(cancellationToken).ConfigureAwait(false);

                Console.WriteLine("Total: {0:N0}", count);
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
