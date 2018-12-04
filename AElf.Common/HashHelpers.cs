using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    public static class HashHelpers
    {
        public static Hash GetDisambiguationHash(ulong blockHeight, Hash pubkeyhash)
        {
            return Hash.Xor(
                Hash.FromMessage(new UInt64Value()
                {
                    Value = blockHeight
                }), pubkeyhash);
        }
    }
}