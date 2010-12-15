﻿using System;
using System.Drawing;
using System.Net;
using System.ServiceModel;

namespace Squiggle.Chat.Services.Chat.Host
{
    [ServiceContract]
    public interface IChatHost
    {
        [OperationContract]
        SessionInfo GetSessionInfo(Guid sessionId, IPEndPoint user);

        [OperationContract]
        void Buzz(Guid sessionId, IPEndPoint user);

        [OperationContract]
        void UserIsTyping(Guid sessionId, IPEndPoint user);

        [OperationContract]
        void ReceiveMessage(Guid sessionId, IPEndPoint user, string fontName, int fontSize, Color color, FontStyle fontStyle, string message);

        [OperationContract]
        void ReceiveChatInvite(Guid sessionId, IPEndPoint user, IPEndPoint[] participants);

        [OperationContract]
        void JoinChat(Guid sessionId, IPEndPoint user);

        [OperationContract]
        void LeaveChat(Guid sessionId, IPEndPoint user);

        [OperationContract]
        void ReceiveFileInvite(Guid sessionId, IPEndPoint user, Guid id, string name, long size);

        [OperationContract]
        void ReceiveFileContent(Guid id, byte[] chunk);

        [OperationContract]
        void AcceptFileInvite(Guid id);

        [OperationContract]
        void CancelFileTransfer(Guid id);
    }
}
