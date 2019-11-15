using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface IBlockTransactionLimitProvider
    {
        Task InitAsync();
        int GetLimit();
        void SetLimit(int limit,Hash blockHash);
        void RemoveForkCache(List<Hash> blockHashes);
        void SetIrreversedCache(List<Hash> blockHashes);
        void SetIrreversedCache(Hash blockHash);
    }
}