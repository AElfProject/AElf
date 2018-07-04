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
                return ByteString.CopyFrom(sd).DeserializeToUInt64();
            }

            if (type == "uint")
            {
                return ByteString.CopyFrom(sd).DeserializeToUInt32();
            }

            if (type == "int")
            {
                return ByteString.CopyFrom(sd).DeserializeToInt32();
            }

            if (type == "long")
            {
                return ByteString.CopyFrom(sd).DeserializeToInt64();
            }
            
            if (type == "bool")
            {
                return ByteString.CopyFrom(sd).DeserializeToBool();
            }

            if (type == "byte[]")
            {
                return ByteString.CopyFrom(sd).DeserializeToBytes().ToHex();
            }

            if (type == "string")
            {
                return ByteString.CopyFrom(sd).DeserializeToString();
            }
            
            return null;
        }
    }
}