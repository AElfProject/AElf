﻿using Google.Protobuf.WellKnownTypes;
using AElf.Types;

namespace AElf.Kernel.SmartContract
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
            ExecutionCallThreshold = SmartContractConstants.ExecutionCallThreshold;
            ExecutionBranchThreshold = SmartContractConstants.ExecutionBranchThreshold;
        }
        public Address Origin { get; set; }
        public Address Miner { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public Hash OriginTransactionId { get; set; }
        public long BlockHeight { get; set; }
        public Timestamp CurrentBlockTime { get; set; }
        public int CallDepth { get; set; }
        public int MaxCallDepth { get; set; }
        public int ExecutionCallThreshold { get; set; }
        public int ExecutionBranchThreshold { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
        public IStateCache StateCache { get; set; }
    }
}
