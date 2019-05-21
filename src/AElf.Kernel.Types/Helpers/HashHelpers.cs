using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
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