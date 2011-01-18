﻿using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Linq;
using Squiggle.Chat.Services.Presence.Transport;
using System.ServiceModel.Channels;

namespace Squiggle.Bridge
{
    class TargetBridge
    {
        public IPEndPoint EndPoint { get; set; }
        public BridgeHostProxy Proxy { get; set; }
    }

    class SquiggleBridge
    {
        BridgeHost bridgeHost = new BridgeHost();
        ServiceHost serviceHost;
        PresenceChannel presenceChannel;
        IPEndPoint bridgeEndPoint;

        List<TargetBridge> targets = new List<TargetBridge>();
        Dictionary<string, TargetBridge> clientBridgeMap = new Dictionary<string, TargetBridge>();
        Dictionary<string, IPEndPoint> localClientEndPoints = new Dictionary<string, IPEndPoint>();

        public SquiggleBridge()
        {
            bridgeHost.PresenceMessageForwarded += new EventHandler<PresenceMessageForwardedEventArgs>(bridgeHost_PresenceMessageForwarded);
        }

        public void AddTarget(IPEndPoint target)
        {
            Uri address;
            Binding binding;
            GetBridgeConnectionParams(target, out address, out binding);
            var proxy = new BridgeHostProxy(binding, new EndpointAddress(address));
            targets.Add(new TargetBridge()
            {
                EndPoint = target,
                Proxy = proxy
            });
        }        

        public void Start(IPEndPoint bridgeEndPoint, IPEndPoint presenceEndPoint)
        {
            this.bridgeEndPoint = bridgeEndPoint;

            Uri address;
            Binding binding;
            GetBridgeConnectionParams(bridgeEndPoint, out address, out binding);
            serviceHost = new ServiceHost(bridgeHost);
            serviceHost.AddServiceEndpoint(typeof(IBridgeHost), binding, address);
            serviceHost.Open();

            presenceChannel = new PresenceChannel(presenceEndPoint, new IPEndPoint(bridgeEndPoint.Address, presenceEndPoint.Port));
            presenceChannel.Start();
            presenceChannel.MessageReceived += new EventHandler<Chat.Services.Presence.Transport.MessageReceivedEventArgs>(presenceChannel_MessageReceived);
            presenceChannel.UserInfoRequested += new EventHandler<Chat.Services.Presence.Transport.Host.UserInfoRequestedEventArgs>(presenceChannel_UserInfoRequested);
        }

        public void Stop()
        {
            presenceChannel.Stop();
            serviceHost.Close();
            foreach (TargetBridge target in targets)
                target.Proxy.Dispose();
        }

        void bridgeHost_PresenceMessageForwarded(object sender, PresenceMessageForwardedEventArgs e)
        {
            if (e.Message.ChannelID != presenceChannel.ChannelID)
            {
                TargetBridge bridge = FindBridge(e.BridgeEndPoint);
                if (bridge != null)
                {
                    clientBridgeMap[e.Message.ClientID] = bridge;
                    e.Message.PresenceEndPoint = bridgeEndPoint;
                    presenceChannel.SendMessage(e.Message);
                }
                Console.WriteLine(e.Message.ToString());
            }
        }

        void presenceChannel_UserInfoRequested(object sender, Chat.Services.Presence.Transport.Host.UserInfoRequestedEventArgs e)
        {
            TargetBridge bridge = FindBridge(e.User.Address);
            if (bridge != null)
                e.UserInfo = bridge.Proxy.GetUserInfo(e.User);
        }

        void presenceChannel_MessageReceived(object sender, Chat.Services.Presence.Transport.MessageReceivedEventArgs e)
        {
            localClientEndPoints[e.Sender.ClientID] = e.Sender.Address;
            e.Sender.Address = bridgeEndPoint;

            byte[] message = e.Message.Serialize();
            foreach (TargetBridge target in targets)
                target.Proxy.ForwardPresenceMessage(message, bridgeEndPoint);
        }

        TargetBridge FindBridge(IPEndPoint endPoint)
        {
            TargetBridge bridge = targets.FirstOrDefault(t => t.EndPoint.Equals(endPoint));
            return bridge;
        }

        void GetBridgeConnectionParams(IPEndPoint endPoint, out Uri address, out Binding binding)
        {
            address = new Uri("net.tcp://" + endPoint.ToString() + "/squigglebridge");
            binding = new NetTcpBinding(SecurityMode.None);
        }
    }
}
