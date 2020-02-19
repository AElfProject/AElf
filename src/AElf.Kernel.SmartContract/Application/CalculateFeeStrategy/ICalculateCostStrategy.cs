using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateCostStrategy
    {
        Task<long> GetCostAsync(IChainContext context, int cost);
        void AddAlgorithm(BlockIndex blockIndex, IList<ICalculateWay> allWay);
        
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
    
    //TODO: we only need one strategy to calculate tx, read, write ... resources.

    public interface ICalculateTxCostStrategy : ICalculateCostStrategy
    {
    }

    public interface ICalculateReadCostStrategy : ICalculateCostStrategy
    {
    }

    public interface ICalculateWriteCostStrategy : ICalculateCostStrategy
    {
    }

    public interface ICalculateStorageCostStrategy : ICalculateCostStrategy
    {
    }

    public interface ICalculateTrafficCostStrategy : ICalculateCostStrategy
    {
    }
}