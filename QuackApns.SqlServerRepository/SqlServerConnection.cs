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
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using QuackApns.Data;
using QuackApns.Random;

namespace QuackApns.SqlServerRepository
{
    public sealed class SqlServerConnection : IDisposable
    {
        readonly SqlConnection _connection;
        readonly IRandomGenerator<ulong> _generator;

        SqlServerConnection(SqlConnection connection, IRandomGenerator<ulong> generator)
        {
            if (null == connection)
                throw new ArgumentNullException("connection");
            if (null == generator)
                throw new ArgumentNullException("generator");

            _connection = connection;
            _generator = generator;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _connection.Dispose();
        }

        #endregion

        public static async Task<SqlServerConnection> ConnectAsync(CancellationToken cancellationToken)
        {
            IRandomGenerator<ulong> generator = new XorShift1024Star();

            var builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["QuackApns"].ConnectionString)
            {
                ContextConnection = false,
                MultipleActiveResultSets = false,
                AsynchronousProcessing = true
            };

            var connectionString = builder.ConnectionString;

            for (var retry = 0; ; ++retry)
            {
                try
                {
                    SqlConnection connection = null;

                    try
                    {
                        connection = new SqlConnection(connectionString);

                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                        //using (var cmd = connection.CreateCommand())
                        //{
                        //    cmd.CommandText = "SELECT @@VERSION, @@SERVERNAME, @@SERVICENAME;";

                        //    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                        //    {
                        //        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        //            throw new InvalidOperationException();

                        //        var version = reader.GetString(0);
                        //        var server = reader.GetString(1);
                        //        var service = reader.GetString(2);

                        //        Console.WriteLine("Connected to {0} ({1})", server, service);
                        //        Console.WriteLine(version);
                        //    }
                        //}

                        var sqlServerConnection = new SqlServerConnection(connection, generator);

                        connection = null;

                        return sqlServerConnection;
                    }
                    finally
                    {
                        if (null != connection)
                            connection.Dispose();
                    }
                }
                catch (SqlException)
                {
                    if (retry > 4)
                        throw;

                    // Check the actual exception so we only retry when there
                    // is some reasonable chance it will do some good.
                }

                var delay = TimeSpan.FromMilliseconds(333 * (1 + retry));

                await generator.RandomDelay(delay, delay + delay, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task LoadAsync(int key, int count, CancellationToken cancellationToken)
        {
            var keyGenerator = new KeyGenerator(key);

            var reader = new KeyDataReader(keyGenerator, count);

            //using (var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction, null)
            using (var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.UseInternalTransaction, null)
            {
                BatchSize = 5000,
                DestinationTableName = "Quack.DeviceRegistrations",
                EnableStreaming = true
            })
            {
                bulkCopy.ColumnMappings.Add("Token", "Token");
                bulkCopy.ColumnMappings.Add("UnixTimestamp", "UnixTimestamp");
                bulkCopy.ColumnMappings.Add("ReceivedUtc", "ReceivedUtc");

                await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<long> GetCountAsync(CancellationToken cancellationToken)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Quack.ApplyRegistrationsTV";
                cmd.CommandTimeout = 120;

                for (var retry = 0; retry < 3; ++retry)
                {
                    SqlException exception;

                    try
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                        break;
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine("Apply failed: " + ex.Message);
                        exception = ex;
                    }

                    SqlInfoMessageEventHandler connectionOnInfoMessage = (o, e) => Console.WriteLine("--> " + e.Message);

                    _connection.InfoMessage += connectionOnInfoMessage;

                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DBCC MEMORYSTATUS;";

                    var sb = new StringBuilder();

                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        using (var file = new FileStream("memory.log", FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                        using (var log = new StreamWriter(file))
                        {
                            await log.WriteLineAsync(DateTimeOffset.Now.ToString()).ConfigureAwait(false);

                            await log.WriteLineAsync(exception.ToString()).ConfigureAwait(false);

                            foreach (SqlError e in exception.Errors)
                            {
                                await log.WriteLineAsync(string.Format("   {0} in {1}:{2} - {3}", e.Server, e.Procedure, e.LineNumber, e.Message)).ConfigureAwait(false);
                            }

                            await log.WriteLineAsync().ConfigureAwait(false);

                            do
                            {
                                sb.Clear();

                                for (var i = 0; i < reader.VisibleFieldCount; ++i)
                                {
                                    sb.AppendFormat("{0, 40}", reader.GetName(i));
                                }

                                await log.WriteLineAsync(sb.ToString()).ConfigureAwait(false);

                                await log.WriteLineAsync("================================================================================").ConfigureAwait(false);

                                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                {
                                    sb.Clear();

                                    for (var i = 0; i < reader.VisibleFieldCount; ++i)
                                    {
                                        sb.AppendFormat("{0, 40}", reader.GetValue(i));
                                    }

                                    await log.WriteLineAsync(sb.ToString()).ConfigureAwait(false);
                                }

                                await log.FlushAsync().ConfigureAwait(false);
                            } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

                            await log.WriteLineAsync().ConfigureAwait(false);
                        }
                    }


                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Quack.ApplyRegistrationsTV";

                    _connection.InfoMessage -= connectionOnInfoMessage;

                    await _generator.RandomDelay(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT COUNT_BIG(*) FROM Quack.Devices;";

                var val = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                return (long)val;
            }
        }

        public async Task<ICollection<ApnsNotification>> GetPendingNotificationsAsync(CancellationToken cancellationToken)
        {
            var notifications = new Dictionary<int, SqlApnsNotification>();
            var notificationSessions = new Dictionary<Tuple<int, int>, SqlApnsNotification>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Quack.GetNotifications";
                cmd.Parameters.AddWithValue("@BatchTimeout", DateTime.UtcNow + TimeSpan.FromMinutes(-20));
                //cmd.Parameters.AddWithValue("@BatchSize", 5000);

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var notificationIdColumn = reader.GetOrdinal("NotificationId");
                    var expirationColumn = reader.GetOrdinal("Expiration");
                    var priorityColumn = reader.GetOrdinal("Priority");
                    var payloadColumn = reader.GetOrdinal("Payload");

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var notificationId = reader.GetInt32(notificationIdColumn);
                        var expiration = reader.GetInt32(expirationColumn);
                        var priority = reader.GetByte(priorityColumn);
                        var payload = reader.GetSqlBytes(payloadColumn).Value;

                        var notification = new SqlApnsNotification
                        {
                            ExpirationEpoch = expiration,
                            Priority = priority,
                            Payload = new ArraySegment<byte>(payload)
                        };

                        notifications[notificationId] = notification;
                    }

                    if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                        throw new InvalidDataException("No devices specified");

                    var notificationSessionIdColumn = reader.GetOrdinal("NotificationSessionId");
                    notificationIdColumn = reader.GetOrdinal("NotificationId");
                    var notificationBatchIdColumn = reader.GetOrdinal("NotificationBatchId");
                    var deviceIdColumn = reader.GetOrdinal("DeviceId");
                    var tokenColumn = reader.GetOrdinal("Token");

                    SqlApnsNotification notificationSession = null;

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var notificationSessionId = reader.GetInt32(notificationSessionIdColumn);
                        var notificationId = reader.GetInt32(notificationIdColumn);
                        var batchId = reader.GetInt32(notificationBatchIdColumn);
                        var deviceId = reader.GetInt32(deviceIdColumn);
                        var token = reader.GetSqlBytes(tokenColumn).Value;

                        if (null == notificationSession
                            || notificationSession.NotificationId != notificationSessionId
                            || notificationSession.BatchId != batchId)
                        {
                            var key = Tuple.Create(notificationSessionId, batchId);

                            if (!notificationSessions.TryGetValue(key, out notificationSession))
                            {
                                var notification = notifications[notificationId];

                                if (0 == notification.NotificationId && null == notification.Devices)
                                {
                                    // We'll just steal this one.  It isn't in use.
                                    notificationSession = notification;
                                }
                                else
                                {
                                    notificationSession = new SqlApnsNotification
                                    {
                                        ExpirationEpoch = notification.ExpirationEpoch,
                                        Priority = notification.Priority,
                                        Payload = notification.Payload,
                                    };
                                }

                                notificationSession.NotificationId = notificationSessionId;
                                notificationSession.BatchId = batchId;
                                notificationSession.Devices = new List<ApnsDevice>();

                                Debug.WriteLine("Adding session " + notificationSession.NotificationId + '/' + notificationSession.BatchId);

                                notificationSessions[key] = notificationSession;
                            }
                        }

                        Debug.Assert(null != notificationSession, "There should not be any null sessions");

                        var device = new SqlApnsDevice(token) { DeviceId = deviceId };

                        notificationSession.WriteableDevices.Add(device);
                    }
                }
            }

            if (notificationSessions.Count < 1)
                return null;

            var values = notificationSessions.Values.OrderBy(ns => ns.NotificationId).ToArray();

#if DEBUG
            foreach (var value in values)
                Debug.WriteLine("Got notification batch " + value.NotificationId + '/' + value.BatchId);
#endif

            return values;
        }

        public async Task FailNotificationAsync(int notificationId, int batchId, CancellationToken cancellationToken)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Quack.AbortBatch";
                cmd.Parameters.AddWithValue("@NotificationSessionId", notificationId);
                cmd.Parameters.AddWithValue("@NotificationBatchId", batchId);

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task CompleteNotificationAsync(int notificationId, int batchId, CancellationToken cancellationToken)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Quack.CompleteBatch";
                cmd.Parameters.AddWithValue("@NotificationSessionId", notificationId);
                cmd.Parameters.AddWithValue("@NotificationBatchId", batchId);

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task CompletePartialNotificationAsync(int notificationId, int batchId, IEnumerable<int> devices, CancellationToken cancellationToken)
        {
            var sqlMetaData = new SqlMetaData("Id", SqlDbType.Int);

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Quack.CompletePartialBatch";
                cmd.Parameters.AddWithValue("@NotificationSessionId", notificationId);
                cmd.Parameters.AddWithValue("@NotificationBatchId", batchId);
                var devicesParam = cmd.Parameters.Add("@Devices", SqlDbType.Structured);

                devicesParam.Value = devices.Select(
                    deviceId =>
                    {
                        var record = new SqlDataRecord(sqlMetaData);

                        record.SetInt32(0, deviceId);

                        return record;
                    });

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
