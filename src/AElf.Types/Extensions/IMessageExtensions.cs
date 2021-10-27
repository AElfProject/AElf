using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf
{
    public static class IMessageExtensions
    {
        public static BytesValue ToBytesValue(this IMessage message)
        {
            return new BytesValue {Value = message.ToByteString()};
        }
    }
}