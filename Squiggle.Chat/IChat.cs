﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Squiggle.Chat
{
    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public Buddy Sender { get; set; }
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public Color Color { get; set; }
        public FontStyle FontStyle { get; set; }
        public string Message { get; set; }
    }

    public class MessageFailedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }

    public class FileTransferInviteEventArgs : EventArgs
    {
        public Buddy Sender { get; set; }
        public IFileTransfer Invitation { get; set; }
    }

    public interface IChat
    {
        IEnumerable<Buddy> Buddies { get; }

        event EventHandler<ChatMessageReceivedEventArgs> MessageReceived;
        event EventHandler<BuddyEventArgs> BuddyJoined;        
        event EventHandler<BuddyEventArgs> BuddyLeft;
        event EventHandler<BuddyEventArgs> BuzzReceived;
        event EventHandler<BuddyEventArgs> BuddyTyping;
        event EventHandler<MessageFailedEventArgs> MessageFailed;
        event EventHandler<FileTransferInviteEventArgs> TransferInvitationReceived;
        event EventHandler GroupChatStarted;

        void NotifyTyping();
        void SendBuzz();
        void SendMessage(string fontName, int fontSize, Color color, FontStyle style, string Message);
        IFileTransfer SendFile(string name, Stream content);
        void Leave();

        void Invite(Buddy buddy);
    }
}
