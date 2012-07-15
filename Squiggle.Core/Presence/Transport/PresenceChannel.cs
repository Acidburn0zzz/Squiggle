﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using Squiggle.Core.Presence.Transport.Host;
using Squiggle.Core.Chat;
using Squiggle.Utilities;
using Squiggle.Core.Presence.Transport.Broadcast;

namespace Squiggle.Core.Presence.Transport
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public SquiggleEndPoint Recipient { get; set; }
        public Message Message { get; set; }

        public bool IsBroadcast
        {
            get { return Recipient == null; }
        }
    }

    public class PresenceChannel: WcfHost
    {
        IBroadcastService broadcastService;
        IPEndPoint serviceEndPoint;
        Dictionary<IPEndPoint, IPresenceHost> presenceHosts;

        PresenceHost presenceHost;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate { };

        public Guid ChannelID { get; private set; }

        public PresenceChannel(IPEndPoint multicastEndPoint, IPEndPoint serviceEndPoint)
        {
            this.ChannelID = Guid.NewGuid();
            if (NetworkUtility.IsMulticast(multicastEndPoint.Address))
            {
                var udpReceiveEndPoint = new IPEndPoint(serviceEndPoint.Address, multicastEndPoint.Port);
                this.broadcastService = new UdpBroadcastService(udpReceiveEndPoint, multicastEndPoint);
            }
            else
                this.broadcastService = new WcfBroadcastService(multicastEndPoint);
            
            this.serviceEndPoint = serviceEndPoint;
            this.presenceHost = new PresenceHost();
            this.presenceHost.MessageReceived += new EventHandler<MessageReceivedEventArgs>(presenceHost_MessageReceived);
            this.presenceHosts = new Dictionary<IPEndPoint, IPresenceHost>();
        }

        protected override void OnStart()
        {
            base.OnStart();

            broadcastService.MessageReceived += new EventHandler<MessageReceivedEventArgs>(broadcastService_MessageReceived);
            broadcastService.Start();
        }

        protected override void OnStop()
        {
            base.OnStop();

            broadcastService.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(broadcastService_MessageReceived);
            broadcastService.Stop();
        }

        public void BroadcastMessage(Message message)
        {
            message.ChannelID = ChannelID;
            broadcastService.SendMessage(message);
        }

        public void SendMessage(Message message, SquiggleEndPoint targetPresenceEndPoint)
        {
            message.ChannelID = ChannelID;
            IPresenceHost host = GetPresenceHost(targetPresenceEndPoint.Address);

            ExceptionMonster.EatTheException(() =>
            {
                host.ReceivePresenceMessage(targetPresenceEndPoint, message.Serialize());
            }, "sending presence message to " + targetPresenceEndPoint);
        }

        protected override ServiceHost CreateHost()
        {
            var serviceHost = new ServiceHost(presenceHost);

            var address = CreateServiceUri(serviceEndPoint.ToString());
            var binding = WcfConfig.CreateBinding();
            serviceHost.AddServiceEndpoint(typeof(IPresenceHost), binding, address);

            return serviceHost;
        }

        void broadcastService_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Async.Invoke(() =>
            {
                OnMessageReceived(e.Recipient, e.Message);
            });
        }   

        void presenceHost_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Async.Invoke(() =>
            {
                OnMessageReceived(e.Recipient, e.Message);
            });
        }

        void OnMessageReceived(SquiggleEndPoint recipient, Message message)
        {
            if (message.IsValid && !message.ChannelID.Equals(ChannelID))
            {
                var args = new MessageReceivedEventArgs()
                {
                    Recipient = recipient,
                    Message = message,
                };
                MessageReceived(this, args);
            }
        }      

        IPresenceHost GetPresenceHost(IPEndPoint endPoint)
        {
            IPresenceHost host;
            lock (presenceHosts)
                if (!presenceHosts.TryGetValue(endPoint, out host))
                {
                    Uri uri = CreateServiceUri(endPoint.ToString());
                    var binding = WcfConfig.CreateBinding();
                    host = new PresenceHostProxy(binding, new EndpointAddress(uri));
                    presenceHosts[endPoint] = host;
                }
            return host;
        }

        static Uri CreateServiceUri(string address)
        {
            var uri = new Uri("net.tcp://" + address + "/" + ServiceNames.PresenceService);
            return uri;
        }
    }
}
