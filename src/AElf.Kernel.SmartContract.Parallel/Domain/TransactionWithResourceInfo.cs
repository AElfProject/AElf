using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class TransactionWithResourceInfo
    {
        public Transaction Transaction { get; set; }
        public TransactionResourceInfo TransactionResourceInfo { get; set; }
    }
}