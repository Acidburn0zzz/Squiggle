﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Squiggle.Core.Presence.Transport;
using Squiggle.Core.Presence.Transport.Messages;
using Squiggle.Core.Chat;
using Squiggle.Utilities;

namespace Squiggle.Core.Presence
{    
    class UserDiscovery
    {
        UserInfo thisUser;
        SquiggleEndPoint localChatEndPoint;
        SquiggleEndPoint localPresenceEndPoint;
        PresenceChannel channel;
        HashSet<UserInfo> onlineUsers;

        public IEnumerable<UserInfo> Users
        {
            get { return onlineUsers; }
        }

        public event EventHandler<UserEventArgs> UserOnline = delegate { };
        public event EventHandler<UserEventArgs> UserOffline = delegate { };
        public event EventHandler<UserEventArgs> UserUpdated = delegate { };
        public event EventHandler<UserEventArgs> UserDiscovered = delegate { };

        public UserDiscovery(PresenceChannel channel)
        {
            this.channel = channel;
            this.onlineUsers = new HashSet<UserInfo>();
        }
        
        public void Login(UserInfo me)
        {
            thisUser = me;

            UnsubscribeChannel();

            channel.MessageReceived += new EventHandler<MessageReceivedEventArgs>(channel_MessageReceived);

            var message = Message.FromSender<LoginMessage>(me);
            channel.BroadcastMessage(message);
            localChatEndPoint = new SquiggleEndPoint(me.ID, me.ChatEndPoint);
            localPresenceEndPoint = new SquiggleEndPoint(me.ID, me.PresenceEndPoint);
        }        

        public void Update(UserInfo me)
        {
            thisUser = me;
            var message = Message.FromSender<UserUpdateMessage>(me);
            channel.BroadcastMessage(message);
        }

        public void FakeLogout(UserInfo me)
        {
            thisUser = me;
            var message = Message.FromSender<LogoutMessage>(me);
            channel.BroadcastMessage(message);
        }

        public void Logout()
        {
            UnsubscribeChannel();

            var message = Message.FromSender<LogoutMessage>(thisUser);
            channel.BroadcastMessage(message);
        }        

        public void DiscoverUser(SquiggleEndPoint user)
        {
            AskForUserInfo(user, UserInfoState.PresenceDiscovered);
        }

        void channel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ExceptionMonster.EatTheException(() =>
                {
                    if (e.Message is LoginMessage)
                        OnLoginMessage(e.Message);
                    else if (e.Message is HiMessage)
                        OnHiMessage((HiMessage)e.Message);
                    else if (e.Message is HelloMessage)
                        OnHelloMessage((HelloMessage)e.Message);
                    else if (e.Message is UserUpdateMessage)
                        OnUpdateMessage((UserUpdateMessage)e.Message);
                    else if (e.Message is GiveUserInfoMessage)
                        OnGiveUserInfoMessage((GiveUserInfoMessage)e.Message);
                    else if (e.Message is UserInfoMessage)
                        OnUserInfoMessage((UserInfoMessage)e.Message);
                    else if (e.Message is LogoutMessage)
                        OnLogoutMessage((LogoutMessage)e.Message);
                    
                }, "handling presence message in userdiscovery class");
        }       

        void OnLogoutMessage(LogoutMessage message)
        {
            IPEndPoint presenceEndPoint = message.PresenceEndPoint;
            OnUserOffline(presenceEndPoint);
        }      

        void OnLoginMessage(Message message)
        {
            var reply = PresenceMessage.FromUserInfo<HiMessage>(thisUser);
            channel.SendMessage(reply, localChatEndPoint, new SquiggleEndPoint(message.ClientID, message.PresenceEndPoint)); 
        }

        void OnHiMessage(HiMessage message)
        {
            var reply = PresenceMessage.FromUserInfo<HelloMessage>(thisUser);
            channel.SendMessage(reply, localChatEndPoint, new SquiggleEndPoint(message.ClientID, message.PresenceEndPoint));

            UserInfo user = message.GetUser();
            OnPresenceMessage(user, true);
        }

        void OnHelloMessage(HelloMessage message)
        {
            OnPresenceMessage(message.GetUser(), discovered: false);
        }

        void OnUpdateMessage(UserUpdateMessage message)
        {
            AskForUserInfo(new SquiggleEndPoint(message.ClientID, message.PresenceEndPoint), UserInfoState.Update);
        }

        void OnUserInfoMessage(UserInfoMessage message)
        {
            var state = (UserInfoState)message.State;
            if (state == UserInfoState.Update)
                OnUserUpdated(message.GetUser());
            else if (state == UserInfoState.PresenceDiscovered)
                OnPresenceMessage(message.GetUser(), discovered: true);
        }

        void OnGiveUserInfoMessage(GiveUserInfoMessage message)
        {
            var reply = PresenceMessage.FromUserInfo<UserInfoMessage>(thisUser);
            reply.State = message.State;
            channel.SendMessage(reply, localChatEndPoint, new SquiggleEndPoint(message.ClientID, message.PresenceEndPoint));
        }        

        void OnPresenceMessage(UserInfo user, bool discovered)
        {
            if (user.Status != UserStatus.Offline)
            {
                if (onlineUsers.Add(user))
                {
                    if (discovered)
                        UserDiscovered(this, new UserEventArgs() { User = user });
                    else
                        UserOnline(this, new UserEventArgs() { User = user });
                }
                else
                    OnUserUpdated(user);
            }
            else
                OnUserOffline(user.PresenceEndPoint);
        }

        void OnUserOffline(IPEndPoint endPoint)
        {
            var user = onlineUsers.FirstOrDefault(u => u.PresenceEndPoint.Equals(endPoint));
            if (user != null)
            {
                onlineUsers.Remove(user);
                UserOffline(this, new UserEventArgs() { User = user });
            }
        }  

        void OnUserUpdated(UserInfo newUser)
        {
            var oldUser = onlineUsers.FirstOrDefault(u => u.Equals(newUser));
            if (oldUser == null)
                OnPresenceMessage(newUser, true);
            else
            {
                oldUser.Update(newUser);
                UserUpdated(this, new UserEventArgs() { User = oldUser });
            }
        }

        void AskForUserInfo(SquiggleEndPoint user, UserInfoState state)
        {
            var reply = Message.FromSender<GiveUserInfoMessage>(thisUser);
            reply.State = (int)state;
            channel.SendMessage(reply, localChatEndPoint, user);
        }

        void UnsubscribeChannel()
        {
            channel.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(channel_MessageReceived);
        }

        enum UserInfoState
        {
            Update = 1,
            PresenceDiscovered = 2
        }
    }
}
