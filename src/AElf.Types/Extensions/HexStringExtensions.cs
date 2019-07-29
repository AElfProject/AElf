using Google.Protobuf;

namespace AElf
{
    public static class HexStringExtensions
    {
        public static ByteString ToByteString(this string hexString)
        {
            return ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(hexString));
        }
    }
}