using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public static class Extensions
    {
        public static UInt64Value ToUInt64Value(this ulong value)
        {
            return new UInt64Value {Value = value};
        }
    }
}