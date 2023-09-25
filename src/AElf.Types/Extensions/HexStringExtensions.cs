using System;
using Google.Protobuf;

namespace AElf
{

    public static class HexStringExtensions
    {
        [Obsolete("Use ByteStringHelper.FromHexString instead.")]
        public static ByteString ToByteString(this string hexString)
        {
            return ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(hexString));
        }
    }
}