﻿using System;
using System.Diagnostics;
using System.Net;
using Squiggle.Utilities;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Squiggle.Chat.Services.Chat;

namespace Squiggle.Chat.Services.Presence.Transport.Host
{
    class PresenceHostProxy: ProxyBase<IPresenceHost>, IPresenceHost
    {
        Binding binding;
        EndpointAddress address;

        public PresenceHostProxy(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress)
        {
            this.binding = binding;
            this.address = remoteAddress;
        }

        protected override ClientBase<IPresenceHost> CreateProxy()
        {
            return new InnerProxy(binding, address);
        }

        public UserInfo GetUserInfo(SquiggleEndPoint user)
        {
            return EnsureProxy<UserInfo>(p=>p.GetUserInfo(user)); 
        }

        public void ReceivePresenceMessage(SquiggleEndPoint sender, SquiggleEndPoint recepient, byte[] message)
        {
            EnsureProxy<object>(p=>{
                p.ReceivePresenceMessage(sender, recepient, message);
                return null;
            });
        }

        class InnerProxy : ClientBase<IPresenceHost>, IPresenceHost
        {            
            public InnerProxy(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress)
                :
                    base(binding, remoteAddress)
            {
            }

            public UserInfo GetUserInfo(SquiggleEndPoint user)
            {
                return base.Channel.GetUserInfo(user); 
            }

            public void ReceivePresenceMessage(SquiggleEndPoint sender, SquiggleEndPoint recepient, byte[] message)
            {
                base.Channel.ReceivePresenceMessage(sender, recepient, message);
            }
        }
    }
}
