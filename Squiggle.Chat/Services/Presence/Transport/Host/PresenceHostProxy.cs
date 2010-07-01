﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Squiggle.Chat.Services.Presence.Transport.Host
{
    class PresenceHostProxy: IPresenceHost
    {
        InnerProxy proxy;
        Binding binding;
        EndpointAddress address;

        public PresenceHostProxy(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress)
        {
            this.binding = binding;
            this.address = remoteAddress;
            EnsureProxy();
        }

        T EnsureProxy<T>(Func<IPresenceHost, T> action)
        {
            EnsureProxy();
            try
            {
                return action(proxy);
            }
            catch (CommunicationException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    EnsureProxy();
                    return action(proxy);
                }
                else
                    throw;
            }
        }

        void EnsureProxy()
        {
            if (proxy == null || 
                proxy.State == CommunicationState.Faulted || 
                proxy.State == CommunicationState.Closed || 
                proxy.State == CommunicationState.Closing)
            {
                if (proxy == null)
                    proxy = new InnerProxy(binding, address);
                else
                {
                    try
                    {
                        proxy.Close();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        proxy.Abort();
                    }
                    finally
                    {
                        proxy = new InnerProxy(binding, address);
                    }
                }
            }
        }       

        public UserInfo GetUserInfo()
        {
            return EnsureProxy<UserInfo>(p=>p.GetUserInfo()); 
        }

        public void ReceiveMessage(IPEndPoint sender, byte[] message)
        {
            EnsureProxy<object>(p=>{
                p.ReceiveMessage(sender, message);
                return null;
            });
        }

        class InnerProxy : ClientBase<IPresenceHost>, IPresenceHost
        {
            public InnerProxy()
            {
            }

            public InnerProxy(string endpointConfigurationName)
                :
                    base(endpointConfigurationName)
            {
            }

            public InnerProxy(string endpointConfigurationName, string remoteAddress)
                :
                    base(endpointConfigurationName, remoteAddress)
            {
            }

            public InnerProxy(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress)
                :
                    base(endpointConfigurationName, remoteAddress)
            {
            }

            public InnerProxy(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress)
                :
                    base(binding, remoteAddress)
            {
            }

            public UserInfo GetUserInfo()
            {
                return base.Channel.GetUserInfo(); 
            }

            public void ReceiveMessage(IPEndPoint sender, byte[] message)
            {
                base.Channel.ReceiveMessage(sender, message);
            }
        }
    }
}
