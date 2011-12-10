﻿using System;
using System.Collections.Generic;
using System.Timers;
using Squiggle.Chat.Services.Presence.Transport;
using Squiggle.Chat.Services.Presence.Transport.Messages;
using Squiggle.Utilities;
using System.Diagnostics;

namespace Squiggle.Chat.Services.Presence
{
    class KeepAliveService : IDisposable
    {
        Timer timer;
        PresenceChannel channel;
        TimeSpan keepAliveSyncTime;
        Message keepAliveMessage;
        Dictionary<UserInfo, DateTime> aliveUsers;
        DateTime lastKeepAliveMessage;

        public event EventHandler<UserEventArgs> UserLost = delegate { };
        public event EventHandler<UserEventArgs> UserDiscovered = delegate { };

        public KeepAliveService(PresenceChannel channel, UserInfo user, TimeSpan keepAliveSyncTime)
        {
            this.channel = channel;
            this.keepAliveSyncTime = keepAliveSyncTime;
            keepAliveMessage = Message.FromUserInfo<KeepAliveMessage>(user);
            aliveUsers = new Dictionary<UserInfo, DateTime>();
        }

        public void Start()
        {
            this.timer = new Timer();
            timer.Interval = keepAliveSyncTime.TotalMilliseconds;
            this.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            this.timer.Start();
            channel.MessageReceived += new EventHandler<MessageReceivedEventArgs>(channel_MessageReceived);
        }

        void channel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message is KeepAliveMessage)
                OnKeepAliveMessage((KeepAliveMessage)e.Message);
        }        

        public void MonitorUser(UserInfo user)
        {
            HeIsAlive(user);
        }

        public void LeaveUser(UserInfo user)
        {
            HeIsGone(user);
        }

        public void HeIsGone(UserInfo user)
        {
            lock (aliveUsers)
                aliveUsers.Remove(user);
        }

        public void HeIsAlive(UserInfo user)
        {
            lock (aliveUsers)
                aliveUsers[user] = DateTime.Now;   
        }

        public void Stop()
        {
            channel.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(channel_MessageReceived);

            lock (aliveUsers)
                aliveUsers.Clear();

            timer.Stop();
            timer = null;
        }

        void ImAlive()
        {
            channel.SendMessage(keepAliveMessage);
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((DateTime.UtcNow - lastKeepAliveMessage).TotalMilliseconds < timer.Interval / 2)
                return;

            lastKeepAliveMessage = DateTime.UtcNow;
            ImAlive();

            List<UserInfo> gone = GetLostUsers();

            foreach (UserInfo user in gone)
                HeIsGone(user);

            foreach (UserInfo user in gone)
                UserLost(this, new UserEventArgs() { User = user });
        }        

        void OnKeepAliveMessage(KeepAliveMessage message)
        {
            var user = new UserInfo() { ID = message.ClientID,
                                        PresenceEndPoint = message.PresenceEndPoint };
            bool existingUser;
            lock (aliveUsers)
                existingUser = aliveUsers.ContainsKey(user);

            if (existingUser)
                HeIsAlive(user);
            else
                UserDiscovered(this, new UserEventArgs() { User = user });
        }

        List<UserInfo> GetLostUsers()
        {
            lock (aliveUsers)
            {
                var now = DateTime.Now;
                List<UserInfo> gone = new List<UserInfo>();
                foreach (KeyValuePair<UserInfo, DateTime> pair in aliveUsers)
                {
                    TimeSpan inactiveTime = now.Subtract(pair.Value);
                    var tolerance = pair.Key.KeepAliveSyncTime + 5.Seconds();
                    TimeSpan waitTime = pair.Key.KeepAliveSyncTime + tolerance;
                    if (inactiveTime > waitTime)
                        gone.Add(pair.Key);
                }
                return gone; 
            }
        }        

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
