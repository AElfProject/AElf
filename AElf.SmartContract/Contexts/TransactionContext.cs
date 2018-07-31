using AElf.Kernel;

namespace AElf.SmartContract
{
    public class TransactionContext : ITransactionContext
    {
        public TransactionContext()
        {
            Origin = new Hash();
            Miner = new Hash();
            PreviousBlockHash = new Hash();
            Transaction = new Transaction();
            Trace = new TransactionTrace();
        }
        public Hash Origin { get; set; }
        public Hash Miner { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
    }
}
