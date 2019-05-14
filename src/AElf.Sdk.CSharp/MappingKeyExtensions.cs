using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public static class MappingKeyExtensions
    {
        public static ByteString ToMappingKey(this string hexString)
        {
            return ByteString.CopyFrom(ByteArrayHelpers.FromHexString(hexString));
        }
    }
}