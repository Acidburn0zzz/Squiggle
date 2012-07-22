﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Squiggle.Core;
using Squiggle.Core.Presence;
using Squiggle.Utilities;
using Squiggle.Utilities.Application;

namespace Squiggle.Chat
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestActivityMonitor();
            TestPresence();
            Console.ReadLine();
        }

        private static void TestActivityMonitor()
        {
            UserActivityMonitor monitor = new UserActivityMonitor(2.Seconds());
            monitor.Idle += (sender, e) => Console.WriteLine("Idle");
            monitor.Active += (sender, e) => Console.WriteLine("Active");
            monitor.Start();
        }

        private static void TestPresence()
        {
            ChatClient client1 = new ChatClient(new SquiggleEndPoint(Guid.NewGuid().ToString(), new IPEndPoint(IPAddress.Loopback, 1234)),
                                                new IPEndPoint(IPAddress.Parse(DefaultValues.PresenceAddress), 12345), 
                                                 new IPEndPoint(IPAddress.Loopback, 1235),
                                                2.Seconds());
            ChatClient client2 = new ChatClient(new SquiggleEndPoint(Guid.NewGuid().ToString(), new IPEndPoint(IPAddress.Loopback, 1236)),
                                                new IPEndPoint(IPAddress.Parse(DefaultValues.PresenceAddress), 12345), 
                                                 new IPEndPoint(IPAddress.Loopback, 1237),
                                                2.Seconds());
            client1.BuddyOnline += new EventHandler<BuddyOnlineEventArgs>(client_BuddyOnline);
            //client2.BuddyOnline += new EventHandler<BuddyOnlineEventArgs>(client_BuddyOnline);
            client2.BuddyOffline += new EventHandler<BuddyEventArgs>(client2_BuddyOffline);
            client2.BuddyUpdated += new EventHandler<BuddyEventArgs>(client2_BuddyUpdated);

            client1.Login("hasan", new BuddyProperties());
            client2.Login("Ali", new BuddyProperties());
            //Thread.Sleep(2000);
            //client2.ChatStarted += new EventHandler<ChatStartedEventArgs>(client2_ChatStarted);
            //var buddy = client1.Buddies.FirstOrDefault();
            //if (buddy != null)
            //chat = client1.StartChat(buddy);
            //chat.SendMessage("Georgia", 12, Colors.Black, "Hello");
            Console.ReadLine();
            client1.Logout();
        }

        static void client2_BuddyUpdated(object sender, BuddyEventArgs e)
        {
            Console.WriteLine(e.Buddy.Properties.Values.FirstOrDefault());
        }

        static void client2_ChatStarted(object sender, ChatStartedEventArgs e)
        {
            e.Chat.TransferInvitationReceived += new EventHandler<FileTransferInviteEventArgs>(Chat_TransferInvitationReceived);
            //chat.SendFile(null, "aloo", File.OpenRead(@"c:\test.txt"));
        }

        static void Chat_TransferInvitationReceived(object sender, FileTransferInviteEventArgs e)
        {
            e.Invitation.Accept(@"d:\dhuz.txt");
        }

        static void client2_BuddyOffline(object sender, BuddyEventArgs e)
        {
            Console.WriteLine("Offline {0}", e.Buddy.DisplayName);
        }

        static void client_BuddyOnline(object sender, BuddyEventArgs e)
        {
            Async.Invoke(() =>
            {
                ((IChatClient)sender).CurrentUser.Properties["Machine"] = "Test2";
            });
            Console.WriteLine("Online {0}", e.Buddy.DisplayName);
            Console.WriteLine(e.Buddy.Properties.Values.First());
        }
    }
}
