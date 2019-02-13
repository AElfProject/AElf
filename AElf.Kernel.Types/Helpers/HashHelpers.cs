using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    public static class HashHelpers
    {
        public static Hash GetDisambiguationHash(ulong blockHeight, Hash pubKeyHash)
        {
            return Hash.Xor(
                Hash.FromMessage(new UInt64Value()
                {
                    Value = blockHeight
                }), pubKeyHash);
        }
    }
}