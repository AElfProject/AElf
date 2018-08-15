using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class IncomingTransaction
    {
        public IncomingTransaction(ITransaction transaction)
        {
            Transaction = transaction;
        }

        public ITransaction Transaction { get; }
    }
}