﻿using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface ITransactionContext
    {
        Hash Origin { get; set; }
        Hash Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        ulong BlockHeight { get; set;}
        ITransaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
    }
}
