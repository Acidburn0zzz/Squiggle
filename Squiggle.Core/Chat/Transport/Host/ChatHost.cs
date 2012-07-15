﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Squiggle.Utilities;
using Squiggle.Core.Chat.Transport.Messages;

namespace Squiggle.Core.Chat.Transport.Host
{
    
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode=ConcurrencyMode.Multiple, UseSynchronizationContext=false)] 
    public class ChatHost: IChatHost
    {
        public event EventHandler<SessionEventArgs> BuzzReceived = delegate { };
        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate { };
        public event EventHandler<SessionEventArgs> UserTyping = delegate { };
        public event EventHandler<ChatInviteReceivedEventArgs> ChatInviteReceived = delegate { };
        public event EventHandler<SessionEventArgs> UserJoined = delegate { };
        public event EventHandler<SessionEventArgs> UserLeft = delegate { };
        public event EventHandler<AppSessionEventArgs> AppInvitationAccepted = delegate { };
        public event EventHandler<AppSessionEventArgs> AppSessionCancelled = delegate { };        
        public event EventHandler<AppInvitationReceivedEventArgs> AppInvitationReceived = delegate { };
        public event EventHandler<AppDataReceivedEventArgs> AppDataReceived = delegate { };
        public event EventHandler<UserActivityEventArgs> UserActivity = delegate { };
        public event EventHandler<SessionEventArgs> SessionInfoRequested = delegate { };
        public event EventHandler<SessionInfoEventArgs> SessionInfoReceived = delegate { };

        #region IChatHost Members

        public void ReceiveChatMessage(SquiggleEndPoint recipient, byte[] message)
        {
            Message obj = Message.Deserialize(message);
            if (obj is AppCancelMessage)
                CancelAppSession(recipient, (AppCancelMessage)obj);
            else if (obj is AppDataMessage)
                ReceiveAppData(recipient, (AppDataMessage)obj);
            else if (obj is AppInviteAcceptMessage)
                AcceptAppInvite(recipient, (AppInviteAcceptMessage)obj);
            else if (obj is AppInviteMessage)
                ReceiveAppInvite(recipient, (AppInviteMessage)obj);
            else if (obj is BuzzMessage)
                Buzz(recipient, (BuzzMessage)obj);
            else if (obj is ChatInviteMessage)
                ReceiveChatInvite(recipient, (ChatInviteMessage)obj);
            else if (obj is ChatJoinMessage)
                JoinChat(recipient, (ChatJoinMessage)obj);
            else if (obj is ChatLeaveMessage)
                LeaveChat(recipient, (ChatLeaveMessage)obj);
            else if (obj is GiveSessionInfoMessage)
                GetSessionInfo(recipient, (GiveSessionInfoMessage)obj);
            else if (obj is SessionInfoMessage)
                ReceiveSessionInfo(recipient, (SessionInfoMessage)obj);
            else if (obj is TextMessage)
                ReceiveMessage(recipient, (TextMessage)obj);
            else if (obj is UserTypingMessage)
                UserIsTyping(recipient, (UserTypingMessage)obj);
        }

        void GetSessionInfo(SquiggleEndPoint recipient, GiveSessionInfoMessage msg)
        {
            SessionInfoRequested(this, new SessionEventArgs(msg.SessionId, msg.Sender));
            Trace.WriteLine(msg.Sender + " is requesting session info.");
        }

        void ReceiveSessionInfo(SquiggleEndPoint recipient, SessionInfoMessage msg)
        {
            SessionInfoReceived(this, new SessionInfoEventArgs() { Participants = msg.Participants.ToArray(), Sender = msg.Sender, SessionID = msg.SessionId });
            Trace.WriteLine(msg.Sender + " is sent session info.");
        }

        void Buzz(SquiggleEndPoint recipient, BuzzMessage msg)
        {
            OnUserActivity(msg.SessionId, msg.Sender, recipient, ActivityType.Buzz);
            BuzzReceived(this, new SessionEventArgs(msg.SessionId, msg.Sender));
            Trace.WriteLine(msg.Sender + " is buzzing.");
        }

        void UserIsTyping(SquiggleEndPoint recipient, UserTypingMessage msg)
        {
            OnUserActivity(msg.SessionId, msg.Sender, recipient, ActivityType.Typing);
            UserTyping(this, new SessionEventArgs(msg.SessionId, msg.Sender ));
            Trace.WriteLine(msg.Sender + " is typing.");
        }

        void ReceiveMessage(SquiggleEndPoint recipient, TextMessage msg)
        {
            OnUserActivity(msg.SessionId, msg.Sender, recipient, ActivityType.Message);
            MessageReceived(this, new MessageReceivedEventArgs()
            {
                SessionID = msg.SessionId, 
                Sender = msg.Sender,
                FontName = msg.FontName,
                FontSize = msg.FontSize,
                Color = msg.Color,
                FontStyle = msg.FontStyle,
                Message = msg.Message 
            });
            Trace.WriteLine("Message received from: " + msg.Sender + ", sessionId= " + msg.SessionId);
        }

        void ReceiveChatInvite(SquiggleEndPoint recipient, ChatInviteMessage msg)
        {
            OnUserActivity(msg.SessionId, msg.Sender, recipient, ActivityType.ChatInvite);
            Trace.WriteLine(msg.Sender + " invited you to group chat.");
            ChatInviteReceived(this, new ChatInviteReceivedEventArgs() 
            { 
                SessionID = msg.SessionId, 
                Sender = msg.Sender, 
                Participants = msg.Participants.ToArray() 
            });
        }

        void JoinChat(SquiggleEndPoint recipient, ChatJoinMessage msg)
        {
            Trace.WriteLine(msg.Sender + " has joined the chat.");
            UserJoined(this, new UserActivityEventArgs() { SessionID = msg.SessionId, Sender = msg.Sender});
        }

        void LeaveChat(SquiggleEndPoint recipient, ChatLeaveMessage msg)
        {
            Trace.WriteLine(msg.Sender + " has left the chat.");
            UserLeft(this, new UserActivityEventArgs() { SessionID = msg.SessionId, Sender = msg.Sender});
        }

        void ReceiveAppInvite(SquiggleEndPoint recipient, AppInviteMessage msg)
        {
            OnUserActivity(msg.SessionId, msg.Sender, recipient, ActivityType.TransferInvite);
            Trace.WriteLine(msg.Sender + " wants to send a file " + msg.Metadata.ToTraceString());
            AppInvitationReceived(this, new AppInvitationReceivedEventArgs()
            {
                SessionID = msg.SessionId,
                Sender = msg.Sender,
                AppId = msg.AppId,
                AppSessionId = msg.AppSessionId,
                Metadata = msg.Metadata.ToDictionary(i => i.Key, i => i.Value)
            });
        }

        void ReceiveAppData(SquiggleEndPoint recipient, AppDataMessage msg)
        {
            AppDataReceived(this, new AppDataReceivedEventArgs() { AppSessionId = msg.SessionId, Chunk = msg.Data });
        }

        void AcceptAppInvite(SquiggleEndPoint recipient, AppInviteAcceptMessage msg)
        {
            AppInvitationAccepted(this, new AppSessionEventArgs() { AppSessionId = msg.SessionId });
        }

        void CancelAppSession(SquiggleEndPoint recipient, AppCancelMessage msg)
        {
            AppSessionCancelled(this, new AppSessionEventArgs() { AppSessionId = msg.SessionId });
        }       

        #endregion

        void OnUserActivity(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, ActivityType type)
        {
            UserActivity(this, new UserActivityEventArgs(){Sender = sender, SessionID = sessionId, Type = type});
        }
    }


    public class SessionEventArgs : EventArgs
    {
        public Guid SessionID { get; set; }
        public SquiggleEndPoint Sender { get; set; }

        public SessionEventArgs(){}

        public SessionEventArgs(Guid sessionId, SquiggleEndPoint user)
        {
            this.SessionID = sessionId;
            this.Sender = user;
        }        
    }

    public class ChatInviteReceivedEventArgs : SessionEventArgs
    {
        public SquiggleEndPoint[] Participants { get; set; }
    }

    public class MessageReceivedEventArgs : SessionEventArgs
    {
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public Color Color { get; set; }
        public FontStyle FontStyle { get; set; }
        public string Message { get; set; }
    }    

    public class AppSessionEventArgs : EventArgs
    {
        public Guid AppSessionId { get; set; }
    }

    public class AppInvitationReceivedEventArgs : SessionEventArgs
    {
        public Guid AppSessionId { get; set; }
        public Guid AppId { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
    }

    public class AppDataReceivedEventArgs : AppSessionEventArgs
    {
        public byte[] Chunk { get; set; }
    }

    public enum ActivityType
    {
        Message,
        Typing,
        Buzz,
        TransferInvite,
        ChatInvite
    }

    public class UserActivityEventArgs : SessionEventArgs
    {
        public ActivityType Type { get; set; }
    }

    public class SessionInfoEventArgs : SessionEventArgs
    {
        public SquiggleEndPoint[] Participants { get; set; }
    }
}
