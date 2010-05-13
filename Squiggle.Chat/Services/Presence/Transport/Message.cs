﻿using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Squiggle.Chat.Services.Presence.Transport
{
    [Serializable]
    class Message
    {
        public byte[] Serialize()
        {
            var stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            return stream.ToArray();
        }

        public static Message Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var stream = new MemoryStream(data);
            var formatter = new BinaryFormatter();
            var message = (Message)formatter.Deserialize(stream);
            return message;
        }
    }
}
