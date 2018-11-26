using System;
using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ITransactionService
    {
        ulong GetPoolSize(string chainId);

        void RecordPoolSize(string chainId, DateTime time, ulong poolSize);

        List<PoolSizeHistory> GetPoolSizeHistory(string chainId);
    }
}