﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Squiggle.Core;
using Squiggle.Core.Chat;
using Squiggle.Core.Chat.Voice;
using Squiggle.Core.Presence;
using Squiggle.History;
using Squiggle.Utilities;

namespace Squiggle.Chat
{
    public class ChatClient: IChatClient
    {
        IChatService chatService;
        IPresenceService presenceService;
        SquiggleEndPoint chatEndPoint;
        BuddyList buddies;

        public event EventHandler<ChatStartedEventArgs> ChatStarted = delegate { };
        public event EventHandler<BuddyOnlineEventArgs> BuddyOnline = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyOffline = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyUpdated = delegate { };

        public Buddy CurrentUser { get; private set; }

        public IEnumerable<Buddy> Buddies 
        {
            get { return buddies; }
        }

        public bool LoggedIn { get; private set; }
        public IVoiceChat ActiveVoiceChat
        {
            get { return chatService.Coalesce(service => service.Sessions.SelectMany(s => s.AppSessions.OfType<IVoiceChat>()).FirstOrDefault(), null); }
        }

        public bool EnableLogging { get; set; }

        public ChatClient(SquiggleEndPoint chatEndPoint, IPEndPoint broadcastEndPoint, IPEndPoint broadcastReceiveEndPoint, IPEndPoint presenceServiceEndPoint, TimeSpan keepAliveTime)
        {
            chatService = new ChatService(chatEndPoint);
            buddies = new BuddyList();
            chatService.ChatStarted += new EventHandler<Squiggle.Core.Chat.ChatStartedEventArgs>(chatService_ChatStarted);
            
            presenceService = new PresenceService(chatEndPoint, broadcastEndPoint, broadcastReceiveEndPoint, presenceServiceEndPoint, keepAliveTime);
            presenceService.UserOffline += new EventHandler<UserEventArgs>(presenceService_UserOffline);
            presenceService.UserOnline += new EventHandler<UserOnlineEventArgs>(presenceService_UserOnline);
            presenceService.UserUpdated += new EventHandler<UserEventArgs>(presenceService_UserUpdated);
            this.chatEndPoint = chatEndPoint;
        }        

        public IChat StartChat(Buddy buddy)
        {
            IChatSession session = chatService.CreateSession(new SquiggleEndPoint(buddy.Id, buddy.ChatEndPoint));
            var chat = new Chat(session, CurrentUser, buddy, id=>buddies[id]);
            return chat;
        }        

        public void Login(string username, BuddyProperties properties)
        {
            username = username.Trim();

            chatService.Start();
            presenceService.Login(username, properties);

            var self = new SelfBuddy(this, chatEndPoint.ClientID, properties) 
            { 
                DisplayName = username,
                Status = UserStatus.Online,
            };
            self.EnableUpdates = true;
            CurrentUser = self;
            LogStatus(self);
            LoggedIn = true;
        }        

        public void Logout()
        {
            LoggedIn = false;
            foreach (Buddy buddy in buddies)
                buddy.Dispose();
            buddies.Clear();
            chatService.Stop();
            presenceService.Logout();

            ((SelfBuddy)CurrentUser).EnableUpdates = false;
            CurrentUser.Status = UserStatus.Offline;
            LogStatus(CurrentUser);
        }
        
        void Update()
        {
            LogStatus(CurrentUser);
            var properties = CurrentUser.Properties.Clone();
            presenceService.Update(CurrentUser.DisplayName, properties, CurrentUser.Status);
        }

        void chatService_ChatStarted(object sender, Squiggle.Core.Chat.ChatStartedEventArgs e)
        {
            var buddyList = new List<Buddy>();
            foreach (SquiggleEndPoint user in e.Session.RemoteUsers)
            {
                Buddy buddy = buddies[user.ClientID];
                if (buddy != null)
                    buddyList.Add(buddy);
            }
            if (buddyList.Count > 0)
            {
                var chat = new Chat(e.Session, CurrentUser, buddyList, id=>buddies[id]);
                ChatStarted(this, new ChatStartedEventArgs() { Chat = chat, Buddies = buddyList });
            }
        }

        void presenceService_UserUpdated(object sender, UserEventArgs e)
        {
            var buddy = buddies[e.User.ID];
            if (buddy != null)
            {
                UserStatus lastStatus = buddy.Status;
                UpdateBuddy(buddy, e.User);

                if (lastStatus != UserStatus.Offline && !buddy.IsOnline)
                    OnBuddyOffline(buddy);
                else if (lastStatus == UserStatus.Offline && buddy.IsOnline)
                    OnBuddyOnline(buddy, false);
                else
                    OnBuddyUpdated(buddy);
            }
        }        

        void presenceService_UserOnline(object sender, UserOnlineEventArgs e)
        {
            var buddy = buddies[e.User.ID];
            if (buddy == null)
            {
                buddy = new Buddy(this, e.User.ID, e.User.ChatEndPoint, new BuddyProperties(e.User.Properties))
                {
                    DisplayName = e.User.DisplayName,
                    Status = e.User.Status,
                };
                buddies.Add(buddy);
            }
            else if (!e.Discovered) // when user is discovered the properties are not available
                UpdateBuddy(buddy, e.User);
            else
                buddy.Status = e.User.Status;
            
            OnBuddyOnline(buddy, e.Discovered);
        }        

        void presenceService_UserOffline(object sender, UserEventArgs e)
        {
            var buddy = buddies[e.User.ID];
            if (buddy != null)
            {
                buddy.Status = UserStatus.Offline;
                OnBuddyOffline(buddy);
            }
        }

        void OnBuddyUpdated(Buddy buddy)
        {
            LogStatus(buddy);
            BuddyUpdated(this, new BuddyEventArgs( buddy ));
        } 

        void OnBuddyOnline(Buddy buddy, bool discovered)
        {
            if (!discovered)
                LogStatus(buddy);
            BuddyOnline(this, new BuddyOnlineEventArgs() { Buddy = buddy, Discovered = discovered });
        }

        void OnBuddyOffline(Buddy buddy)
        {
            LogStatus(buddy);
            BuddyOffline(this, new BuddyEventArgs( buddy ));
        }

        void UpdateBuddy(Buddy buddy, UserInfo user)
        {
            buddy.Status = user.Status;
            buddy.DisplayName = user.DisplayName;
            buddy.Update(user.ChatEndPoint, user.Properties);
        }

        void LogStatus(Buddy buddy)
        {
            if (EnableLogging)
                ExceptionMonster.EatTheException(() =>
                {
                    var manager = new HistoryManager();
                    manager.AddStatusUpdate(DateTime.Now, new Guid(buddy.Id), buddy.DisplayName, (int)buddy.Status);
                }, "logging history.");
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

            public SelfBuddy(IChatClient client, string id, BuddyProperties properties) : base(client, id, null, properties) { }

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

            protected override void OnBuddyPropertiesChanged()
            {
                base.OnBuddyPropertiesChanged();
                Update();
            }

            void Update()
            {
                if (EnableUpdates)
                    ((ChatClient)base.ChatClient).Update();
            }
        }
    }
}
