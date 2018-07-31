using AElf.Kernel;

namespace AElf.Sdk.CSharp.ReadOnly
{
    public static class Extensions
    {
        public static Transaction ToReadOnly(this Transaction transaction)
        {
            // TODO: Transaction may be replaced with Transaction
            return ((Transaction)transaction).Clone();
        }

        public static Hash ToReadOnly(this Hash hash)
        {
            return hash.Clone();
        }
    }
}