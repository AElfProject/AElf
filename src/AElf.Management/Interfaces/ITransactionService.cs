using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ITransactionService
    {
        Task<int> GetPoolSize(string chainId);

        Task RecordPoolSize(string chainId, DateTime timee);

        Task<List<PoolSizeHistory>> GetPoolSizeHistory(string chainId);
    }
}