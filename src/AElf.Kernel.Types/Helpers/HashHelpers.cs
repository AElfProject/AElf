using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    public static class HashHelpers
    {
        public static Hash GetDisambiguationHash(long blockHeight, Hash pubKeyHash)
        {
            return Hash.Xor(
                Hash.FromMessage(new Int64Value()
                {
                    Value = blockHeight
                }), pubKeyHash);
        }
    }
}