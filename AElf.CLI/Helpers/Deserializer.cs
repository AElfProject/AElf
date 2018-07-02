using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CLI.Helpers
{
    public class Deserializer
    {
        public object Deserialize(string type, byte[] sd)
        {
            if (type == "ulong")
            {
                return sd.DeserializeToUInt64();
            }

            if (type == "uint")
            {
                return sd.DeserializeToUInt32();
            }

            if (type == "int")
            {
                return sd.DeserializeToInt32();
            }

            if (type == "long")
            {
                return sd.DeserializeToInt64();
            }
            
            if (type == "bool")
            {
                return sd.DeserializeToBool();
            }

            if (type == "byte[]")
            {
                return sd.DeserializeToBytes().ToHex();
            }

            if (type == "string")
            {
                return sd.DeserializeToString();
            }
            
            return null;
        }
    }
}