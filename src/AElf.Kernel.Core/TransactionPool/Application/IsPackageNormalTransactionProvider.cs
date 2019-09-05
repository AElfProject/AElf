namespace AElf.Kernel.TransactionPool.Application
{
    public class IsPackageNormalTransactionProvider : IIsPackageNormalTransactionProvider
    {
        public bool IsPackage { get; set; } = true;
    }
}