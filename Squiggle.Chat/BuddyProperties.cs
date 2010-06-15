﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Squiggle.Chat
{
    public class BuddyProperties
    {
        Dictionary<string, string> dictionary;

        public event EventHandler Changed = delegate { };

        public BuddyProperties(Dictionary<string, string> properties)
        {
            this.dictionary = properties;
        }

        public string this[string key]
        {
            get
            {
                string value;
                dictionary.TryGetValue(key, out value);
                return value;
            }
            set
            {
                dictionary[key] = value;
                Changed(this, EventArgs.Empty);
            }
        }

        public Dictionary<string,string>.KeyCollection Keys
        {
            get { return dictionary.Keys; }
        }

        public Dictionary<string, string>.ValueCollection Values
        {
            get { return dictionary.Values; }
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(dictionary);
        }
    }
}
