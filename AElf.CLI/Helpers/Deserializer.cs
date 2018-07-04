using System;
using System.Text;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CLI.Helpers
{
    public class Deserializer
    {
        // TODO: Remove this, it's not needed any more
        public object Deserialize(string type, byte[] sd)
        {
            if (type == "ulong")
            {
                return BitConverter.ToUInt64(sd, 0);
            }

            if (type == "uint")
            {
                return BitConverter.ToUInt32(sd, 0);
            }

            if (type == "int")
            {
                return BitConverter.ToInt32(sd, 0);
            }

            if (type == "long")
            {
                return BitConverter.ToInt64(sd, 0);
            }
            
            if (type == "bool")
            {
                return BitConverter.ToBoolean(sd, 0);
            }

            if (type == "byte[]")
            {
                return sd.ToString();
            }

            if (type == "string")
            {
                return Encoding.UTF8.GetString(sd);
            }
            
            return null;
        }
    }
}