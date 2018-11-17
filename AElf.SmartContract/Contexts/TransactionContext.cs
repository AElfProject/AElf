﻿using AElf.Kernel;
using AElf.Common;

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
            BlockHeight = 0;
            CallDepth = 0;
        }
        public Hash Origin { get; set; }
        public Hash Miner { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong BlockHeight { get; set; }
        public int CallDepth { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
    }
}
