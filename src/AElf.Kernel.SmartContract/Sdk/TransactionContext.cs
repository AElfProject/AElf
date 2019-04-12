using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class TransactionContext : ITransactionContext
    {
        public TransactionContext()
        {
            Origin = new Address();
            Miner = new Address();
            PreviousBlockHash = new Hash();
            Transaction = new Transaction();
            Trace = new TransactionTrace();
            BlockHeight = 0;
            CallDepth = 0;
            MaxCallDepth = 4; // Default max call depth 4
        }
        public Address Origin { get; set; }
        public Address Miner { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public long BlockHeight { get; set; }
        public DateTime CurrentBlockTime { get; set; }
        public int CallDepth { get; set; }
        public int MaxCallDepth { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
        public IStateCache StateCache { get; set; }
    }
}
