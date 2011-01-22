﻿using System.Configuration;

namespace Squiggle.Bridge.Configuration
{
    class BridgeConfiguration : ConfigurationSection
    {
        public static BridgeConfiguration GetConfig()
        {
            return (BridgeConfiguration)ConfigurationManager.GetSection("BridgeConfiguration") ?? new BridgeConfiguration();
        }

        [ConfigurationProperty("remoteservicebinding")]
        public Target RemoteServiceBinding
        {
            get { return (Target)this["remoteservicebinding"] ?? new Target(); }
        }

        [ConfigurationProperty("localservicebinding")]
        public Target LocalServiceBinding
        {
            get { return (Target)this["localservicebinding"] ?? new Target(); }
        }

        [ConfigurationProperty("channelbinding")]
        public Target ChannelBinding
        {
            get { return (Target)this["channelbinding"] ?? new Target(); }
        }

        [ConfigurationProperty("targets")]
        public TargetCollection Targets
        {
            get
            {
                return (TargetCollection)this["targets"] ??
                   new TargetCollection();
            }
        }

    }
}
