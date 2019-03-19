using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    //TODO: Add test case for HashHelpers [Case]
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