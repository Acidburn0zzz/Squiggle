﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squiggle.Chat.Services.Chat.Host;
using System.Net;
using System.IO;

namespace Squiggle.Chat.Services
{
    public class FileTransferInviteEventArgs: EventArgs
    {
        public IFileTransfer Invitation {get; set; }
    }

    public interface IChatSession
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<UserEventArgs> UserTyping;
        event EventHandler<FileTransferInviteEventArgs> TransferInvitationReceived;

        IPEndPoint RemoteUser { get; set; }

        void SendMessage(string message);        
        void NotifyTyping();
        IFileTransfer SendFile(string name, Stream content);
        void End();
    }
}
