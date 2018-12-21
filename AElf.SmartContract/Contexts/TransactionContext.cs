﻿using AElf.Kernel;
using AElf.Common;

namespace AElf.SmartContract
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
        }
        public Address Origin { get; set; }
        public Address Miner { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong BlockHeight { get; set; }
        public int CallDepth { get; set; }
        public Transaction Transaction { get; set; }
        public TransactionTrace Trace { get; set; }
    }
}
