using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    public static class HashHelpers
    {
        public static Hash GetDisambiguationHash(ulong blockHeight, Address producerAddress)
        {
            return Hash.Xor(
                Hash.FromMessage(new UInt64Value()
                {
                    Value = blockHeight
                }),
                Hash.FromRawBytes(producerAddress.DumpByteArray()));
        }
    }
}