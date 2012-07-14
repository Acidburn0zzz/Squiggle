﻿using System;
using System.ServiceProcess;
using Squiggle.Bridge.Configuration;
using System.Net;
using System.Diagnostics;
using Squiggle.Utilities;

namespace Squiggle.Bridge
{
    public partial class SquiggleBridgeService : ConsoleService
    {
        SquiggleBridge bridge;

        public SquiggleBridgeService()
        {
            InitializeComponent();
        }                

        protected override void OnStart(string[] args)
        {
            var config = BridgeConfiguration.GetConfig();

            var channelServiceEndPoint = new IPEndPoint(config.InternalServiceBinding.EndPoint.Address, config.ChannelBinding.ServicePort);

            DumpConfig(config, channelServiceEndPoint);

            bridge = new SquiggleBridge(config.InternalServiceBinding.EndPoint,
                                        config.ExternalServiceBinding.EndPoint,
                                        config.ChannelBinding.MulticastEndPoint,
                                        channelServiceEndPoint);

            foreach (Target target in config.Targets)
                bridge.AddTarget(target.EndPoint);

            bridge.Start();
        }

        static void DumpConfig(BridgeConfiguration config, IPEndPoint channelServiceEndPoint)
        {
            Trace.WriteLine(":: Settings ::");
            Trace.WriteLine("");
            Trace.WriteLine("Internal bridge endpoint: " + config.InternalServiceBinding.EndPoint);
            Trace.WriteLine("External bridge endpoint: " + config.ExternalServiceBinding.EndPoint);
            Trace.WriteLine("Presence multicast endpoint: " + config.ChannelBinding.MulticastEndPoint);
            Trace.WriteLine("Presence endpoint: " + channelServiceEndPoint);
            Trace.WriteLine("");
            Trace.WriteLine(":: Target bridges ::");
            Trace.WriteLine("");
            foreach (Target target in config.Targets)
                Trace.WriteLine(target.EndPoint);
            Trace.WriteLine("");
        }

        protected override void OnStop()
        {
            bridge.Stop();
            bridge = null;
        }
    }
}
