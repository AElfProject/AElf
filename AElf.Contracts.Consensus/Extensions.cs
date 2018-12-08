using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus
{
    public static class Extensions
    {
        public static UInt64Value ToUInt64Value(this ulong value)
        {
            return new UInt64Value {Value = value};
        }
    }
}