﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squiggle.Client;
using Squiggle.UI.Settings;
using System.Net;
using Squiggle.Utilities;
using Squiggle.UI.Resources;
using Squiggle.Utilities.Net;
using Squiggle.Core;

namespace Squiggle.UI.Factories
{
    class ChatClientOptionsFactory: IInstanceFactory<ChatClientOptions>
    {
        SquiggleSettings settings;

        public ChatClientOptionsFactory(SquiggleSettings settings)
        {
            this.settings = settings;
        }

        public ChatClientOptions CreateInstance()
        {
            int chatPort = settings.ConnectionSettings.ChatPort;
            if (String.IsNullOrEmpty(settings.ConnectionSettings.BindToIP))
                throw new OperationCanceledException(Translation.Instance.Error_NoNetwork);

            var localIP = IPAddress.Parse(settings.ConnectionSettings.BindToIP);
            TimeSpan keepAliveTimeout = settings.ConnectionSettings.KeepAliveTime.Seconds();

            IPAddress presenceAddress;
            if (!NetworkUtility.TryParseAddress(settings.ConnectionSettings.PresenceAddress, out presenceAddress))
                throw new ApplicationException(Translation.Instance.SettingsWindow_Error_InvalidPresenceIP);

            var chatEndPoint = NetworkUtility.GetFreeEndPoint(new IPEndPoint(localIP, chatPort));
            var presenceServiceEndPoint = NetworkUtility.GetFreeEndPoint(new IPEndPoint(localIP, settings.ConnectionSettings.PresencePort));
            var multicastEndPoint = new IPEndPoint(presenceAddress, settings.ConnectionSettings.PresenceCallbackPort);
            var multicastReceiveEndPoint = NetworkUtility.GetFreeEndPoint(new IPEndPoint(localIP, settings.ConnectionSettings.PresenceCallbackPort));

            string clientID = settings.ConnectionSettings.ClientID;

            var options = new ChatClientOptions()
            {
                ChatEndPoint = new SquiggleEndPoint(clientID, chatEndPoint),
                MulticastEndPoint = multicastEndPoint,
                MulticastReceiveEndPoint = multicastReceiveEndPoint,
                PresenceServiceEndPoint = presenceServiceEndPoint,
                KeepAliveTime = keepAliveTimeout
            };
            
            return options;
        }
    }
}
