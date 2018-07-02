using AElf.Kernel;

namespace AElf.Sdk.CSharp.ReadOnly
{
    public static class Extensions
    {
        public static ITransaction ToReadOnly(this ITransaction transaction)
        {
            // TODO: ITransaction may be replaced with Transaction
            return ((Transaction)transaction).Clone();
        }

        public static Hash ToReadOnly(this Hash hash)
        {
            return hash.Clone();
        }
    }
}