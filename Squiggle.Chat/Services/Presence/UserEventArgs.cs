﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Squiggle.Chat.Services.Presence
{
    class UserEventArgs : EventArgs
    {
        public UserInfo User {get; set; }
    }
}
