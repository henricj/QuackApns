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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using QuackApns.Data;
using QuackApns.Random;
using QuackApns.Utility;
using StackExchange.Redis;

namespace QuackApns.RedisRepository
{
    public sealed class RedisConnection : IDisposable
    {
        const string UpdateScript = @"
for i, key in ipairs(KEYS) do
   local newTime = ARGV[1]
   if 0 == redis.call('HSETNX', key, 'timestamp', newTime) then
      local timestamp = redis.call('HGET', key, 'timestamp')
      if newTime > timestamp then
         redis.call('HSET', key, 'timestamp', newTime)
         redis.call('HDEL', key, 'deregistered')
      end
   end
   redis.call('SADD', 'devices', key)
end
";

        static readonly byte[] DevicePrefix = Encoding.UTF8.GetBytes("device:");
        static readonly byte[] EventPrefix = Encoding.UTF8.GetBytes("event:");
        readonly ConnectionMultiplexer _redis;

        RedisConnection(ConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _redis.Dispose();
        }

        #endregion

        public static async Task<RedisConnection> ConnectAsync(CancellationToken cancellationToken)
        {
            var options = new ConfigurationOptions { WriteBuffer = 64 * 1024, SyncTimeout = 30 * 1000 };

            options.EndPoints.Add("sark.int.henric.info");

            var redis = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);

            redis.PreserveAsyncOrder = false;

            return new RedisConnection(redis);
        }

        async Task KillAllAsync()
        {
            foreach (var ep in _redis.GetEndPoints(true))
            {
                var server = _redis.GetServer(ep);

                var clients = await server.ClientListAsync().ConfigureAwait(false);

                foreach (var client in clients)
                    await server.ClientKillAsync(client.Address).ConfigureAwait(false);
            }
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var lengthTask = db.SetLengthAsync("devices");

                var count = await db.ScriptEvaluateAsync("return #redis.pcall('keys', 'device:*')").ConfigureAwait(false);

                var length = await lengthTask.ConfigureAwait(false);

                if (count.IsNull)
                    return 0;

                if (length != (int)count)
                {
                    Console.WriteLine("Mismatch: {0} != {1}", length, count);

                    await RebuildDeviceSetAsync(cancellationToken).ConfigureAwait(false);
                }

                return (int)count;
            }
        }

        public async Task RebuildDeviceSetAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                await db.KeyDeleteAsync("devices").ConfigureAwait(false);

                var tasks = _redis.GetEndPoints(true)
                    .Select(ep => _redis.GetServer(ep))
                    .Select(s => Task.Run(() => RebuildDeviceSet(db, s, cancellationToken), cancellationToken))
                    .ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        void RebuildDeviceSet(IDatabase database, IServer server, CancellationToken cancellationToken)
        {
            try
            {
                var keys = server.Keys(pattern: "device:*", pageSize: 10000);

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var key in keys)
                    database.SetAdd("devices", (byte[])key, CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RebuildDeviceSetAsync failed: " + ex.Message);
            }
        }

        public async Task LoadAsync(int seed, int count, CancellationToken cancellationToken)
        {
            var keyGenerator = new KeyGenerator(seed);

            var now = DateTimeOffset.UtcNow.ToInt32UnixEpoch();

            var bufferBuffer = new BufferBlock<byte[]>();

            for (var i = 0; i < 500; ++i)
            {
                var key = new byte[DevicePrefix.Length + 32];

                Array.Copy(DevicePrefix, 0, key, 0, DevicePrefix.Length);

                bufferBuffer.Post(key);
            }

            var rngBlock = new TransformBlock<byte[], RedisKey>(b =>
            {
                keyGenerator.Next(b, DevicePrefix.Length, b.Length - DevicePrefix.Length);

                return b;
            });

            var batchBlock = new BatchBlock<RedisKey>(10);

            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var scriptKey = await LoadScriptAsync(UpdateScript).ConfigureAwait(false);

                var deviceCount = 0;
                var addBlock = new ActionBlock<RedisKey[]>(b =>
                {
                    if (deviceCount < count)
                    {
                        var task = AddDeviceAsync(db, scriptKey, b, now)
                            .ContinueWith(t =>
                            {
                                foreach (var x in b)
                                    bufferBuffer.Post((byte[])x);
                            },
                                TaskContinuationOptions.ExecuteSynchronously);
                    }

                    if (Interlocked.Add(ref deviceCount, b.Length) >= count)
                        rngBlock.Complete();
                }, new ExecutionDataflowBlockOptions { BoundedCapacity = 4 });


                batchBlock.LinkTo(addBlock, new DataflowLinkOptions { PropagateCompletion = true });

                rngBlock.LinkTo(batchBlock, new DataflowLinkOptions { PropagateCompletion = true });

                bufferBuffer.LinkTo(rngBlock, new DataflowLinkOptions { PropagateCompletion = true });

                await addBlock.Completion.ConfigureAwait(false);
            }
        }

        Task<byte[]> LoadScriptAsync(string script)
        {
            var server = _redis.GetServer(_redis.GetEndPoints(true).FirstOrDefault());

            return server.ScriptLoadAsync(script);
        }

        static void ReportFailures(IEnumerable<Task> tasks)
        {
            foreach (var t in tasks)
            {
                var ex = t.Exception;

                if (null != ex)
                    Debug.WriteLine("Device add failed: " + ex.Message);
            }
        }

        int GetTimestamp(RedisValue value)
        {
            int n;
            if (int.TryParse(value, out n))
                return n;

            return int.MinValue;
        }

        async Task AddDeviceAsync(IDatabase db, byte[] scriptKey, RedisKey[] keys, int now)
        {
            try
            {
                await db.ScriptEvaluateAsync(scriptKey, keys, new RedisValue[] { now });

                //var setKeyTask = db.SetAddAsync("devices", key);

                //if (!await db.HashSetAsync(key, "timestamp", now, When.NotExists).ConfigureAwait(false))
                //{
                //    var timestamp = await db.HashGetAsync(key, "timestamp").ConfigureAwait(false);

                //    if (!timestamp.HasValue || GetTimestamp(timestamp) < now)
                //    {
                //        await db.HashSetAsync(key, "timestamp", now).ConfigureAwait(false);
                //    }
                //}

                //await setKeyTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Add failed: " + ex.Message);
            }
        }

        public async Task<int> DeregisterRandomDevicesAsync(IRandomGenerator generator, int count, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow.ToInt32UnixEpoch();

            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var keys = await db.SetRandomMembersAsync("devices", count).ConfigureAwait(false);

                var events = new RedisValue[keys.Length];

                var tasks = new List<Task>(2 * keys.Length + 1);

                for (var i = 0; i < keys.Length; ++i)
                {
                    // We should check the timestamp before making a change...

                    var key = keys[i];

                    var deregTask = db.HashSetAsync((byte[])key, new[] { new HashEntry("deregistered", 1), new HashEntry("timestamp", now) });

                    tasks.Add(deregTask);

                    var eventKey = CreateEventKey();

                    events[i] = eventKey;

                    var eventTask = AddEventAsync(db, eventKey, now, key, "deregister");

                    tasks.Add(eventTask);
                }

                var reportTask = db.ListRightPushAsync("deregister", events);

                tasks.Add(reportTask);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                return keys.Length;
            }
        }

        static Task AddEventAsync(IDatabase db, RedisKey eventKey, int now, RedisValue deviceKey, string message)
        {
            return db.HashSetAsync(eventKey, new[] { new HashEntry("timestamp", now), new HashEntry("device", deviceKey), new HashEntry("event", message) });
        }

        static byte[] CreateEventKey()
        {
            var timestamp = Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("yyyyMMdd HHmmss.fff"));

            var eventKey = new byte[EventPrefix.Length + timestamp.Length + 1 + 16];

            Array.Copy(EventPrefix, 0, eventKey, 0, EventPrefix.Length);
            Array.Copy(timestamp, 0, eventKey, EventPrefix.Length, timestamp.Length);

            eventKey[EventPrefix.Length + timestamp.Length] = (byte)':';

            var eventId = Guid.NewGuid();

            eventId.ToByteArray().CopyTo(eventKey, EventPrefix.Length + timestamp.Length + 1);

            return eventKey;
        }

        public async Task<int> RegisterRandomDevicesAsync(IRandomGenerator generator, int count, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow.ToInt32UnixEpoch();

            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var scriptKey = await LoadScriptAsync(UpdateScript).ConfigureAwait(false);

                var tasks = new List<Task>(count);

                var events = new RedisValue[count];
                var keys = new RedisKey[count];

                for (var i = 0; i < count; ++i)
                {
                    var key = new byte[DevicePrefix.Length + ApnsConstants.DeviceTokenLength];

                    Array.Copy(DevicePrefix, 0, key, 0, DevicePrefix.Length);

                    generator.GetBytes(key, DevicePrefix.Length, key.Length - DevicePrefix.Length);

                    keys[i] = key;

                    var eventKey = CreateEventKey();

                    events[i] = eventKey;

                    var eventTask = AddEventAsync(db, eventKey, now, key, "register");

                    tasks.Add(eventTask);
                }

                await AddDeviceAsync(db, scriptKey, keys, now).ConfigureAwait(false);

                var registerTask = db.ListRightPushAsync("register", events);

                tasks.Add(registerTask);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                return keys.Length;
            }
        }

        public Task<ICollection<RegistrationEvent>> GetPendingDeregistrationsAsync(CancellationToken cancellationToken)
        {
            return GetPendingAsync("deregister", cancellationToken);
        }

        public Task ClearPendingDeregistrationsAsync(ICollection<RegistrationEvent> events, CancellationToken cancellationToken)
        {
            return ClearPendingAsync(events, "deregister", cancellationToken);
        }

        public Task<ICollection<RegistrationEvent>> GetPendingRegistrationsAsync(CancellationToken cancellationToken)
        {
            return GetPendingAsync("register", cancellationToken);
        }

        public Task ClearPendingRegistrationsAsync(ICollection<RegistrationEvent> events, CancellationToken cancellationToken)
        {
            return ClearPendingAsync(events, "register", cancellationToken);
        }


        async Task<ICollection<RegistrationEvent>> GetPendingAsync(RedisKey key, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var eventKeys = await db.ListRangeAsync(key).ConfigureAwait(false);

                var eventValues = new RedisValue[] { "timestamp", "device" };

                var events = await Task.WhenAll(eventKeys.Select(ek => db.HashGetAsync((byte[])ek, eventValues))).ConfigureAwait(false);

                if (null == events || 0 == events.Length)
                    return null;

                var registrationEvents = new List<RegistrationEvent>(events.Length);

                var tasks = new List<Task>();

                for (var i = 0; i < events.Length; ++i)
                {
                    var registration = events[i];

                    var eventKey = eventKeys[i];

                    if (null != registration)
                    {
                        try
                        {
                            var timestamp = (int)registration[0];
                            var device = (byte[])registration[1];

                            var registrationEvent = new RegistrationEvent(eventKey, device, timestamp);

                            registrationEvents.Add(registrationEvent);

                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Unable to process event: " + ex.Message);
                        }

                        var removeEvent = db.KeyDeleteAsync((byte[])eventKey);

                        tasks.Add(removeEvent);
                    }

                    var removeDeregisterTask = db.ListRemoveAsync(key, eventKey, 1);

                    tasks.Add(removeDeregisterTask);
                }

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks).ConfigureAwait(false);

                return registrationEvents;
            }
        }

        async Task ClearPendingAsync(ICollection<RegistrationEvent> events, RedisKey key, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(obj => ((ConnectionMultiplexer)obj).Close(false), _redis))
            {
                var db = _redis.GetDatabase();

                var tasks = new List<Task>(events.Count * 2);

                foreach (var registration in events)
                {
                    var eventKey = registration.EventKey;

                    var listRemoveTask = db.ListRemoveAsync(key, eventKey, 1);

                    tasks.Add(listRemoveTask);

                    var removeEvent = db.KeyDeleteAsync(eventKey);

                    tasks.Add(removeEvent);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        #region Nested type: RegistrationEvent

        public class RegistrationEvent
        {
            readonly byte[] _deviceKey;
            readonly byte[] _eventKey;
            readonly int _unixTimestamp;

            public RegistrationEvent(byte[] eventKey, byte[] deviceKey, int unixTimestamp)
            {
                _eventKey = eventKey;
                _deviceKey = deviceKey;
                _unixTimestamp = unixTimestamp;
            }

            internal byte[] EventKey
            {
                get { return _eventKey; }
            }

            internal byte[] DeviceKey
            {
                get { return _deviceKey; }
            }

            public int UnixTimestamp
            {
                get { return _unixTimestamp; }
            }

            public void GetDeviceKey(byte[] buffer, int offset, int count)
            {
                Array.Copy(_deviceKey, DevicePrefix.Length, buffer, offset, count);
            }
        }

        #endregion
    }
}
