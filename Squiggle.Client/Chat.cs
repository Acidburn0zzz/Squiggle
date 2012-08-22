﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using Squiggle.Core;
using Squiggle.Core.Chat;
using Squiggle.History;
using Squiggle.History.DAL;
using Squiggle.Utilities;
using BuddyResolver = System.Func<string, Squiggle.Client.Buddy>;
using Squiggle.Core.Chat.Activity;
using Squiggle.Activity;

namespace Squiggle.Client
{    
    class Chat: IChat
    {
        IChatSession session;
        ChatBuddies buddies;
        IBuddy self;

        public Chat(IChatSession session, IBuddy self, IBuddy buddy, BuddyResolver buddyResolver) : this(session, self, Enumerable.Repeat(buddy, 1), buddyResolver) { }

        public Chat(IChatSession session, IBuddy self, IEnumerable<IBuddy> buddies, BuddyResolver buddyResolver)
        {
            this.self = self;
            
            this.buddies = new ChatBuddies(session, buddyResolver, buddies);
            this.buddies.BuddyJoined += new EventHandler<BuddyEventArgs>(buddies_BuddyJoined);
            this.buddies.BuddyLeft += new EventHandler<BuddyEventArgs>(buddies_BuddyLeft);
            this.buddies.GroupChatStarted += new EventHandler(buddies_GroupChatStarted);

            this.session = session;
            session.MessageReceived += new EventHandler<Squiggle.Core.Chat.TextMessageReceivedEventArgs>(session_MessageReceived);
            session.UserTyping += new EventHandler<Squiggle.Core.Chat.SessionEventArgs>(session_UserTyping);
            session.BuzzReceived += new EventHandler<Squiggle.Core.Chat.SessionEventArgs>(session_BuzzReceived);
            session.ActivityInviteReceived += new EventHandler<ActivityInivteReceivedEventArgs>(session_ActivityInviteReceived);
        }             

        #region IChat Members

        public IEnumerable<IBuddy> Buddies
        {
            get { return buddies; }
        }

        public bool IsGroupChat
        {
            get { return session.IsGroupSession; }
        }

        public bool EnableLogging { get; set; }

        public event EventHandler<ChatMessageReceivedEventArgs> MessageReceived = delegate { };
        public event EventHandler<MessageFailedEventArgs> MessageFailed = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyJoined = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyLeft = delegate { };
        public event EventHandler<BuddyEventArgs> BuddyTyping = delegate { };
        public event EventHandler<BuddyEventArgs> BuzzReceived = delegate { };
        public event EventHandler<ActivityInvitationReceivedEventArgs> ActivityInvitationReceived = delegate { };
        public event EventHandler GroupChatStarted = delegate { };

        public void SendMessage(string fontName, int fontSize, Color color, FontStyle fontStyle, string message)
        {
            Async.Invoke(() =>
            {
                Exception ex;
                if (!ExceptionMonster.EatTheException(()=>
                                    {
                                        session.SendMessage(fontName, fontSize, color, fontStyle, message);                    
                                    }, "sending chat message", out ex))
                    MessageFailed(this, new MessageFailedEventArgs()
                    {
                        Message = message,
                        Exception = ex
                    });
                LogHistory(EventType.Message, self, message);
            });
        }

        public void NotifyTyping()
        {
            Async.Invoke(() => 
            {
                ExceptionMonster.EatTheException(() => session.NotifyTyping(), "sending typing message");
            });
        }

        public void SendBuzz()
        {
            Async.Invoke(() =>
            {
                ExceptionMonster.EatTheException(() => session.SendBuzz(), "sending buzz");
                LogHistory(EventType.Buzz, self);
            });
        }

        public IActivityExecutor CreateActivity()
        {
            if (IsGroupChat)
                throw new InvalidOperationException("Can not start activity session in group chat.");

            return session.CreateActivity();
        }        

        public void Leave()
        {
            Async.Invoke(() =>
            {
                ExceptionMonster.EatTheException(() => session.End(), "leaving chat");
                LogHistory(EventType.Left, self);
            });
        }

        public void Invite(IBuddy buddy)
        {
            Async.Invoke(() =>
            {
                var endpoint = new SquiggleEndPoint(buddy.Id, buddy.ChatEndPoint);
                ExceptionMonster.EatTheException(() => session.Invite(endpoint), "sending chat invite to " + endpoint);
            });
        }

        #endregion

        void buddies_GroupChatStarted(object sender, EventArgs e)
        {
            GroupChatStarted(this, EventArgs.Empty);
        }

        void session_ActivityInviteReceived(object sender, ActivityInivteReceivedEventArgs e)
        {
            IBuddy buddy;
            if (buddies.TryGet(e.Sender.ClientID, out buddy))
            {
                var args = new ActivityInvitationReceivedEventArgs(buddy)
                {
                    Executor = e.Executor,
                    ActivityId = e.ActivityId,
                    Metadata = e.Metadata
                };
                ActivityInvitationReceived(this, args);
                LogHistory(EventType.Activity, buddy);
            }
        }  

        void session_BuzzReceived(object sender, Squiggle.Core.Chat.SessionEventArgs e)
        {
            IBuddy buddy;
            if (buddies.TryGet(e.Sender.ClientID, out buddy))
            {
                BuzzReceived(this, new BuddyEventArgs(buddy));
                LogHistory(EventType.Buzz, buddy);
            }
        } 

        void session_UserTyping(object sender, Squiggle.Core.Chat.SessionEventArgs e)
        {
            IBuddy buddy;
            if (buddies.TryGet(e.Sender.ClientID, out buddy))
                BuddyTyping(this, new BuddyEventArgs( buddy ));
        }

        void buddies_BuddyLeft(object sender, BuddyEventArgs e)
        {
            BuddyLeft(this, e);
            LogHistory(EventType.Left, e.Buddy);
        }

        void buddies_BuddyJoined(object sender, BuddyEventArgs e)
        {
            BuddyJoined(this, e);
            LogHistory(EventType.Joined, e.Buddy);
        }

        void session_MessageReceived(object sender, Squiggle.Core.Chat.TextMessageReceivedEventArgs e)
        {
            IBuddy buddy;
            if (buddies.TryGet(e.Sender.ClientID, out buddy))
            {
                MessageReceived(this, new ChatMessageReceivedEventArgs()
                {
                    Sender = buddy,
                    FontName = e.FontName,
                    FontSize = e.FontSize,
                    Color = e.Color,
                    FontStyle = e.FontStyle,
                    Message = e.Message
                });
                LogHistory(EventType.Message, buddy, e.Message);
            }
        }

        bool sessionLogged;
        void LogHistory(EventType eventType, IBuddy sender, string data = null)
        {
            DoHistoryAction(manager=>
            {
                if (!sessionLogged)
                    LogSessionStart();
                manager.AddSessionEvent(session.ID, DateTime.Now, eventType, new Guid(sender.Id), sender.DisplayName, buddies.Select(b => new Guid(b.Id)), data);
            });
        }

        void LogSessionStart()
        {
            DoHistoryAction(manager =>
            {
                sessionLogged = true;
                IBuddy primaryBuddy = Buddies.FirstOrDefault();
                var newSession = Session.CreateSession(session.ID, new Guid(primaryBuddy.Id), primaryBuddy.DisplayName, DateTime.Now);
                manager.AddSession(newSession, Buddies.Select(b => Participant.CreateParticipant(Guid.NewGuid(), new Guid(b.Id), b.DisplayName)));
            });
        }

        void DoHistoryAction(Action<HistoryManager> action)
        {
            if (EnableLogging)
                ExceptionMonster.EatTheException(() => action(new HistoryManager()), "logging history.");
        }      
    }
}
