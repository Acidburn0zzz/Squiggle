﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Squiggle.Core.Chat;
using Squiggle.Core.Presence.Transport;
using Squiggle.Utilities;
using Squiggle.Utilities.Threading;

namespace Squiggle.Core.Presence
{
    public class PresenceService : IPresenceService
    {
        UserDiscovery discovery;
        PresenceChannel channel;
        KeepAliveService keepAlive;

        UserInfo thisUser;

        public event EventHandler<UserOnlineEventArgs> UserOnline = delegate { };
        public event EventHandler<UserEventArgs> UserOffline = delegate { };
        public event EventHandler<UserEventArgs> UserUpdated = delegate { };

        public IEnumerable<IUserInfo> Users
        {
            get { return discovery.Users; }
        }

        public PresenceService(PresenceServiceOptions options)
        {
            thisUser = new UserInfo()
            {
                ID = options.ChatEndPoint.ClientID,
                ChatEndPoint = options.ChatEndPoint.Address,
                KeepAliveSyncTime = options.KeepAliveTime,
                PresenceEndPoint = options.PresenceServiceEndPoint
            };

            channel = new PresenceChannel(options.MulticastEndPoint, options.MulticastReceiveEndPoint, options.PresenceServiceEndPoint);

            this.discovery = new UserDiscovery(channel);
            discovery.UserOnline += new EventHandler<UserEventArgs>(discovery_UserOnline);
            discovery.UserOffline += new EventHandler<UserEventArgs>(discovery_UserOffline);
            discovery.UserUpdated += new EventHandler<UserEventArgs>(discovery_UserUpdated);
            discovery.UserDiscovered += new EventHandler<UserEventArgs>(discovery_UserDiscovered);

            this.keepAlive = new KeepAliveService(channel, thisUser, options.KeepAliveTime);
            this.keepAlive.UserLost += new EventHandler<UserEventArgs>(keepAlive_UserLost);
            this.keepAlive.UserDiscovered += new EventHandler<UserEventArgs>(keepAlive_UserDiscovered);
        }        

        public void Login(string name, IBuddyProperties properties)
        {
            thisUser.DisplayName = name;
            thisUser.Status = UserStatus.Online;
            thisUser.Properties = properties.ToDictionary();

            channel.Start();
            discovery.Login(thisUser.Clone());
            keepAlive.Start();
        }

        public void Update(string name, IBuddyProperties properties, UserStatus status)
        {
            UserStatus lastStatus = thisUser.Status;

            UserInfo userInfo;
            lock (thisUser)
            {
                thisUser.DisplayName = name;
                thisUser.Status = status;
                thisUser.Properties = properties.ToDictionary();

                userInfo = thisUser.Clone();
            }

            if (lastStatus == UserStatus.Offline)
            {
                if (status == UserStatus.Offline)
                    return;
                else
                    discovery.Login(userInfo);
            }
            else if (status == UserStatus.Offline)
                discovery.FakeLogout(userInfo);
            else
                discovery.Update(userInfo);
        }

        public void Logout()
        {
            discovery.Logout();
            keepAlive.Stop();
            channel.Stop();
        }    

        void keepAlive_UserDiscovered(object sender, UserEventArgs e)
        {
            if (ResolveUser(e))
            {
                keepAlive.MonitorUser(e.User);
                OnUserOnline(e, true);
            }
            else
                discovery.DiscoverUser(new SquiggleEndPoint(e.User.ID, e.User.PresenceEndPoint));
        }

        void keepAlive_UserLosing(object sender, UserEventArgs e)
        {
            if (ResolveUser(e))
                Async.Invoke(() =>
                {
                    discovery.DiscoverUser(new SquiggleEndPoint(e.User.ID, e.User.PresenceEndPoint));
                });
        }

        void keepAlive_UserLost(object sender, UserEventArgs e)
        {
            if (ResolveUser(e))
                OnUserOffline(e);
        }        

        void discovery_UserUpdated(object sender, UserEventArgs e)
        {
            keepAlive.HeIsAlive(e.User);
            UserUpdated(this, e);
        }
        
        void discovery_UserOnline(object sender, UserEventArgs e)
        {
            keepAlive.MonitorUser(e.User);
            OnUserOnline(e, false);
        }

        void discovery_UserDiscovered(object sender, UserEventArgs e)
        {
            keepAlive.MonitorUser(e.User);
            OnUserOnline(e, true);
        }   

        void discovery_UserOffline(object sender, UserEventArgs e)
        {
            keepAlive.LeaveUser(e.User);
            OnUserOffline(e);
        }

        void OnUserOnline(UserEventArgs e, bool discovered)
        {                
            UserOnline(this, new UserOnlineEventArgs() { User = e.User, Discovered = discovered });
        }

        void OnUserOffline(UserEventArgs e)
        {
            UserOffline(this, e);
        }

        bool ResolveUser(UserEventArgs e)
        {
            // userinfo coming from keepaliveservice only has presenceendpoint. Complete information is with discovery service.
            IUserInfo user = discovery.Users.FirstOrDefault(u => u.Equals(e.User));
            if (user != null)
                e.User = user;
            return user != null;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Logout();
        }

        #endregion
    }    
}
