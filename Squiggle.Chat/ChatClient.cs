﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Squiggle.Chat.Services.Presence;
using Squiggle.Chat.Services.Chat;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Squiggle.Chat.Services;

namespace Squiggle.Chat
{
    public class ChatClient: IChatClient
    {
        IChatService chatService;
        IPresenceService presenceService;
        IPEndPoint localEndPoint;
        BuddyList buddies;

        public event EventHandler<ChatStartedEventArgs> ChatStarted = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyOnline = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyOffline = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyUpdated = delegate { };

        public Buddy CurrentUser { get; private set; }

        public IEnumerable<Buddy> Buddies 
        {
            get { return buddies; }
        }

        public ChatClient(IPEndPoint localEndPoint, short presencePort, TimeSpan keepAliveTime)
        {
            chatService = new ChatService();
            buddies = new BuddyList();
            chatService.ChatStarted += new EventHandler<Squiggle.Chat.Services.ChatStartedEventArgs>(chatService_ChatStarted);
            presenceService = new PresenceService(localEndPoint, presencePort, keepAliveTime);
            presenceService.UserOffline += new EventHandler<UserEventArgs>(presenceService_UserOffline);
            presenceService.UserOnline += new EventHandler<UserEventArgs>(presenceService_UserOnline);
            presenceService.UserUpdated += new EventHandler<UserEventArgs>(presenceService_UserUpdated);
            this.localEndPoint = localEndPoint;
        }        

        public IChat StartChat(Buddy buddy)
        {
            var endpoint = (IPEndPoint)buddy.ID;
            IChatSession session = chatService.CreateSession(endpoint);
            var chat = new Chat(session, buddy);
            return chat;
        }

        public void EndChat(Buddy buddy)
        {
            var endpoint = (IPEndPoint)buddy.ID;
            chatService.RemoveSession(endpoint);
        }

        public void Login(string username)
        {
            chatService.Username = username;
            chatService.Start(localEndPoint);
            presenceService.Login(username, String.Empty);

            var self = new SelfBuddy(this, localEndPoint) 
            { 
                DisplayName = username, 
                DisplayMessage = String.Empty,
                Status = UserStatus.Online 
            };
            self.EnableUpdates = true;
            CurrentUser = self;
        }

        private void Update()
        {
            presenceService.Update(CurrentUser.DisplayName, CurrentUser.DisplayMessage, CurrentUser.Status);
        }

        public void Logout()
        {
            foreach (Buddy buddy in buddies)
                buddy.Dispose();
            buddies.Clear();
            chatService.Stop();
            presenceService.Logout();
        }

        void chatService_ChatStarted(object sender, Squiggle.Chat.Services.ChatStartedEventArgs e)
        {
            Buddy buddy = buddies[e.Session.RemoteUser];
            var chat = new Chat(e.Session, buddy);
            ChatStarted(this, new ChatStartedEventArgs() { Chat = chat,Buddy = buddy, Message = e.Message });
        }

        void presenceService_UserUpdated(object sender, UserEventArgs e)
        {
            var buddy = buddies[e.User.ChatEndPoint];
            if (buddy != null)
            {
                buddy.Status = e.User.Status;
                buddy.DisplayMessage = e.User.DisplayMessage;
                buddy.DisplayName = e.User.DisplayMessage;
                BuddyUpdated(this, new BuddyEventArgs() { Buddy = buddy });
            }
        }       

        void presenceService_UserOnline(object sender, UserEventArgs e)
        {
            var buddy = buddies[e.User.ChatEndPoint];
            if (buddy == null)
            {
                buddy = new Buddy(this, e.User.ChatEndPoint)
                {
                    DisplayName = e.User.UserFriendlyName,
                    Status = e.User.Status,
                    DisplayMessage = e.User.DisplayMessage,
                };
                System.Diagnostics.Debug.WriteLine(buddy.DisplayName);
                buddies.Add(buddy);
                BuddyOnline(this, new BuddyEventArgs() { Buddy = buddy });
            }
        }

        void presenceService_UserOffline(object sender, UserEventArgs e)
        {
            var buddy = buddies[e.User.ChatEndPoint];
            if (buddy == null)
                BuddyOffline(this, new BuddyEventArgs(){Buddy = buddy});
        }        

        void OnBuddyStatusChanged(Buddy buddy)
        {
            var args = new BuddyEventArgs() { Buddy = buddy };
            if (buddy.Status == UserStatus.Online)
                BuddyOnline(this, args);
            else if (buddy.Status == UserStatus.Offline)
                BuddyOffline(this, args);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Logout();
        }

        #endregion

        class SelfBuddy : Buddy
        {
            public bool EnableUpdates { get; set; }

            public SelfBuddy(IChatClient client, IPEndPoint id) : base(client, id) { }

            public override string DisplayMessage
            {
                get
                {
                    return base.DisplayMessage;
                }
                set
                {
                    base.DisplayMessage = value;
                    Update();
                }
            }
            public override string DisplayName
            {
                get
                {
                    return base.DisplayName;
                }
                set
                {
                    base.DisplayName = value;
                    Update();
                }
            }
            public override UserStatus Status
            {
                get
                {
                    return base.Status;
                }
                set
                {
                    base.Status = value;
                    Update();
                }
            }

            void Update()
            {
                if (EnableUpdates)
                    ((ChatClient)base.ChatClient).Update();
            }
        }
    }
}
