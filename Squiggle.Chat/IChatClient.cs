﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Squiggle.Chat
{
    public interface IChatClient
    {
        event EventHandler<ChatStartedEventArgs> ChatStarted;
        event EventHandler<BuddyEventArgs> BuddyOnline;
        event EventHandler<BuddyEventArgs> BuddyOffline;

        Buddy CurrentUser{get; }
        IEnumerable<Buddy> Buddies { get; }

        IChatSession StartChat(Buddy buddy);
        void EndChat(Buddy buddy);
        void Login(string username);
        void Logout();
    }
}
