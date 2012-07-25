﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squiggle.Chat;
using Squiggle.UI.Settings;
using System.Net;
using Squiggle.Utilities;
using Squiggle.UI.Resources;
using Squiggle.Utilities.Net;
using Squiggle.Core;

namespace Squiggle.UI.Helpers
{
    class ChatClientFactory
    {
        SquiggleSettings settings;

        public ChatClientFactory(SquiggleSettings settings)
        {
            this.settings = settings;
        }

        public IChatClient CreateClient()
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
            var broadcastEndPoint = new IPEndPoint(presenceAddress, settings.ConnectionSettings.PresenceCallbackPort);
            var broadcastReceiveEndPoint = NetworkUtility.GetFreeEndPoint(new IPEndPoint(localIP, settings.ConnectionSettings.PresenceCallbackPort));

            string clientID = settings.ConnectionSettings.ClientID;

            var client = new ChatClient(new SquiggleEndPoint(clientID, chatEndPoint), broadcastEndPoint, broadcastReceiveEndPoint, presenceServiceEndPoint, keepAliveTimeout);
            client.EnableLogging = settings.GeneralSettings.EnableStatusLogging;

            return client;
        }
    }
}
