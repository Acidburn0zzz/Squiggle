﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Squiggle.Core.Chat.Transport.Messages;

namespace Squiggle.Core.Chat.Transport.Host
{
    public static class ChatHostExtensions
    {
        public static void GetSessionInfo(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new GiveSessionInfoMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient });
        }

        public static void ReceiveSessionInfo(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, SquiggleEndPoint[] participants)
        {
            host.Send(new SessionInfoMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient, Participants = participants.ToList() });
        }

        public static void Buzz(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new BuzzMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient });
        }

        public static void UserIsTyping(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new UserTypingMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient });
        }

        public static void ReceiveMessage(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, string fontName, int fontSize, Color color, FontStyle fontStyle, string message)
        {
            host.Send(new TextMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient, FontName = fontName, FontSize = fontSize, Color = color, FontStyle = fontStyle, Message = message });
        }

        public static void ReceiveChatInvite(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, IEnumerable<SquiggleEndPoint> participants)
        {
            host.Send(new ChatInviteMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient, Participants = participants.ToList() });
        }

        public static void JoinChat(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new ChatJoinMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient });
        }

        public static void LeaveChat(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new ChatLeaveMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient });
        }

        public static void ReceiveActivityInvite(this ChatHost host, Guid sessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, Guid activityId, Guid activitySessionId, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            host.Send(new ActivityInviteMessage() { SessionId = sessionId, Sender = sender, Recipient = recipient, ActivityId = activityId, ActivitySessionId = activitySessionId, Metadata = metadata.ToDictionary(kv => kv.Key, kv => kv.Value) });
        }

        public static void ReceiveActivityData(this ChatHost host, Guid activitySessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient, byte[] chunk)
        {
            host.Send(new ActivityDataMessage() { SessionId = activitySessionId, Sender = sender, Recipient = recipient, Data = chunk });
        }

        public static void AcceptActivityInvite(this ChatHost host, Guid activitySessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new ActivityInviteAcceptMessage() { SessionId = activitySessionId, Sender = sender, Recipient = recipient });
        }

        public static void CancelActivitySession(this ChatHost host, Guid activitySessionId, SquiggleEndPoint sender, SquiggleEndPoint recipient)
        {
            host.Send(new ActivityCancelMessage() { SessionId = activitySessionId, Sender = sender, Recipient = recipient });
        }
    }
}
