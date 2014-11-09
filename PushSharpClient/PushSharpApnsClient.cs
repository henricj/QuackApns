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
using System.Threading;
using System.Threading.Tasks;
using PushSharp;
using PushSharp.Apple;
using PushSharp.Core;
using QuackApns;
using QuackApns.Certificates;
using QuackApns.Random;
using QuackApns.Utility;

namespace PushSharpClient
{
    public class PushSharpApnsClient
    {
        const string ClientP12File = "server.p12";

        public async Task PushSharpAsync(string host, int port, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            const int count = 10 * 1000;

            var notifications = CreateNotificationBatch(count);

            sw.Stop();

            var createElapsed = sw.Elapsed;

            Console.WriteLine("Created {0:N3} kMsg in {1} ({2:F2}kMsg/s)", count / 1000.0, createElapsed, count / createElapsed.TotalMilliseconds);

            var certificate = await IsolatedStorageCertificates.GetCertificateAsync("Client", ClientP12File, true, cancellationToken).ConfigureAwait(false);

            var push = new PushBroker();

            push.OnNotificationSent += NotificationSent;
            push.OnChannelException += ChannelException;
            push.OnServiceException += ServiceException;
            push.OnNotificationFailed += NotificationFailed;
            push.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            push.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
            push.OnChannelCreated += ChannelCreated;
            push.OnChannelDestroyed += ChannelDestroyed;

            var settings = new ApplePushChannelSettings(certificate, true);

            settings.OverrideServer(host, port);
            settings.OverrideFeedbackServer(host, port + 1);

            push.RegisterAppleService(settings);

            sw = Stopwatch.StartNew();

            foreach (var n in notifications)
                push.QueueNotification(n);

            var queueElapsed = sw.Elapsed;

            //Console.WriteLine("Waiting for Queue to Finish...");

            //Stop and wait for the queues to drains
            push.StopAllServices();

            var doneElapsed = sw.Elapsed - queueElapsed;

            Console.WriteLine("Wrote {0:N3} kMsg in {1} ({2} queue {3} wait)", count / 1000.0, sw.Elapsed, queueElapsed, doneElapsed);
            Console.WriteLine("Wrote {0:F2} kMsg/s", count / sw.Elapsed.TotalMilliseconds);
        }

        static ICollection<AppleNotification> CreateNotificationBatch(int count)
        {
            var deviceTokenConverter = new DeviceTokenConverter();

            var deviceToken = new byte[ApnsConstants.DeviceTokenLength];

            var rng = new XorShift1024Star();

            var notifications = new AppleNotification[count];

            for (var i = 0; i < notifications.Length; i++)
            {
                rng.GetBytes(deviceToken);

                var binaryToken = deviceTokenConverter.TokenToString(deviceToken);

                notifications[i] = new AppleNotification(binaryToken)
                    .WithAlert("Testing " + rng.Next() + " at " + DateTimeOffset.Now);
            }

            return notifications;
        }

        static void DeviceSubscriptionChanged(object sender, string oldSubscriptionId, string newSubscriptionId, INotification notification)
        {
            //Currently this event will only ever happen for Android GCM
            Console.WriteLine("Device Registration Changed:  Old-> " + oldSubscriptionId + "  New->+" + newSubscriptionId + " -> " + notification);
        }

        static void NotificationSent(object sender, INotification notification)
        {
            /*SqlConnection conn = new SqlConnection(CONNECTION_STRING);

            using (conn)
            {
                conn.Open();

                using (SqlCommand comm = new SqlCommand(@"exec InsertSessionLog " + newSessionId + "
+, '" + deviceToken + "' ", conn))
                {
                    comm.ExecuteNonQuery();
                }
            }*/

            //Console.WriteLine("Sent: " + sender + " -> " + notification);
            //LogMessageToFileSend("Sent: " + sender + " -> " + notification);
        }

        static void NotificationFailed(object sender, INotification notification, Exception notificationFailureException)
        {
            Console.WriteLine("Failure: " + sender + " -> " + notificationFailureException.Message + " -> " + notification);
        }

        static void ChannelException(object sender, IPushChannel channel, Exception exception)
        {
            Console.WriteLine("Channel Exception: " + sender + " -> " + exception);
        }

        static void ServiceException(object sender, Exception exception)
        {
            Console.WriteLine("Channel Exception: " + sender + " -> " + exception);
        }

        static void DeviceSubscriptionExpired(object sender, string expiredDeviceSubscriptionId, DateTime timestamp, INotification notification)
        {
            Console.WriteLine("Device Subscription Expired: " + sender + " -> " + expiredDeviceSubscriptionId);
        }

        static void ChannelDestroyed(object sender)
        {
            Console.WriteLine("Channel Destroyed for: " + sender);
        }

        static void ChannelCreated(object sender, IPushChannel pushChannel)
        {
            Console.WriteLine("Channel Created for: " + sender);
        }

        public static void LogMessageToFile(string msg)
        {
            /*if (!System.IO.File.Exists("apns_log.txt"))
              {
                  System.IO.FileStream f = System.IO.File.Create("apns_log.txt");
                  f.Close();
              }*/

            var sw = File.AppendText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apns_log_iphone.txt"));
            try
            {
                var logLine = String.Format("{0:G}: {1}.", DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                sw.Close();
            }
        }

        public static void LogMessageToFileSend(string msg)
        {
            /*if (!System.IO.File.Exists("apns_log.txt"))
              {
                  System.IO.FileStream f = System.IO.File.Create("apns_log.txt");
                  f.Close();
              }*/

            var sw = File.AppendText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apns_log_iphone_sent.txt"));
            try
            {
                var logLine = String.Format(
                    "{0:G}: {1}.", DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}
