﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Squiggle.Utilities;

namespace Squiggle.Core.Chat.Host
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

        public void GetSessionInfo(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            SessionInfoRequested(this, new SessionEventArgs(sessionId, sender));
            Trace.WriteLine(sender + " is requesting session info.");
        }

        public void ReceiveSessionInfo(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, SessionInfo sessionInfo)
        {
            SessionInfoReceived(this, new SessionInfoEventArgs() { Info = sessionInfo, Sender = sender, SessionID = sessionId });
            Trace.WriteLine(sender + " is sent session info.");
        }

        public void Buzz(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            OnUserActivity(sessionId, sender, recipient, ActivityType.Buzz);
            BuzzReceived(this, new SessionEventArgs(sessionId, sender ));
            Trace.WriteLine(sender + " is buzzing.");
        }

        public void UserIsTyping(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            OnUserActivity(sessionId, sender, recipient, ActivityType.Typing);
            UserTyping(this, new SessionEventArgs(sessionId, sender ));
            Trace.WriteLine(sender + " is typing.");
        }

        public void ReceiveMessage(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, string fontName, int fontSize, Color color, FontStyle fontStyle, string message)
        {
            OnUserActivity(sessionId, sender, recipient, ActivityType.Message);
            MessageReceived(this, new MessageReceivedEventArgs(){SessionID = sessionId, 
                                                                                          Sender = sender,
                                                                                          FontName = fontName,
                                                                                          FontSize = fontSize,
                                                                                          Color = color,
                                                                                          FontStyle = fontStyle,
                                                                                          Message = message });
            Trace.WriteLine("Message received from: " + sender + ", sessionId= " + sessionId);
        }

        public void ReceiveChatInvite(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, SquiggleEndPoint[] participants)
        {
            OnUserActivity(sessionId, sender, recipient, ActivityType.ChatInvite);
            Trace.WriteLine(sender + " invited you to group chat.");
            ChatInviteReceived(this, new ChatInviteReceivedEventArgs() 
            { 
                SessionID = sessionId, 
                Sender = sender, 
                Participants = participants 
            });
        }

        public void JoinChat(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            Trace.WriteLine(sender + " has joined the chat.");
            UserJoined(this, new UserActivityEventArgs() { SessionID = sessionId, Sender = sender});
        }

        public void LeaveChat(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            Trace.WriteLine(sender + " has left the chat.");
            UserLeft(this, new UserActivityEventArgs() { SessionID = sessionId, Sender = sender});
        }

        public void ReceiveAppInvite(Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, Guid appId, Guid appSessionId, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            OnUserActivity(sessionId, sender, recipient, ActivityType.TransferInvite);
            Trace.WriteLine(sender + " wants to send a file " + metadata.ToTraceString());
            AppInvitationReceived(this, new AppInvitationReceivedEventArgs()
            {
                SessionID = sessionId,
                Sender = sender,
                AppId = appId,
                AppSessionId = appSessionId,
                Metadata = metadata.ToDictionary(i=>i.Key, i=>i.Value)
            });
        }

        public void ReceiveAppData(Guid appSessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, byte[] chunk)
        {
            AppDataReceived(this, new AppDataReceivedEventArgs() { AppSessionId = appSessionId, Chunk = chunk });
        }

        public void AcceptAppInvite(Guid appSessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            AppInvitationAccepted(this, new AppSessionEventArgs() { AppSessionId = appSessionId });
        }

        public void CancelAppSession(Guid appSessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            AppSessionCancelled(this, new AppSessionEventArgs() { AppSessionId = appSessionId });
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
        public SessionInfo Info { get; set; }
    }
}
