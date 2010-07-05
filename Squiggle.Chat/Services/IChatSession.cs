﻿using System;
using System.Drawing;
using System.IO;
using System.Net;
using Squiggle.Chat.Services.Chat.Host;
using System.Collections.Generic;

namespace Squiggle.Chat.Services
{
    public class FileTransferInviteEventArgs: SessionEventArgs
    {
        public IFileTransfer Invitation {get; set; }
    }

    public interface IChatSession
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<SessionEventArgs> BuzzReceived;
        event EventHandler<SessionEventArgs> UserTyping;
        event EventHandler<FileTransferInviteEventArgs> TransferInvitationReceived;

        IEnumerable<IPEndPoint> RemoteUsers { get; }
        Guid ID { get; }
        bool IsGroupSession { get; }

        void SendBuzz();
        void SendMessage(string fontName, int fontSize, Color color, FontStyle fontStyle, string message);
        void NotifyTyping();
        IFileTransfer SendFile(string name, Stream content);
        void End();
    }
}
