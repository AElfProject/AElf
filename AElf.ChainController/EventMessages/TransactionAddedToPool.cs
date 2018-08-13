using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class TransactionAddedToPool
    {
        public TransactionAddedToPool(ITransaction transaction)
        {
            Transaction = transaction;
        }

        public ITransaction Transaction { get; }
    }
}