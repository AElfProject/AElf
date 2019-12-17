using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateCostStrategy
    {
        Task<long> GetCostAsync(IChainContext context, int cost);
        void AddAlgorithm(BlockIndex blockIndex, IList<ICalculateWay> allWay);
        
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public interface ICalculateTxCostStrategy : ICalculateCostStrategy, ISingletonDependency
    {
    }

    public interface ICalculateCpuCostStrategy : ICalculateCostStrategy, ISingletonDependency
    {
    }

    public interface ICalculateRamCostStrategy : ICalculateCostStrategy, ISingletonDependency
    {
    }

    public interface ICalculateStoCostStrategy : ICalculateCostStrategy, ISingletonDependency
    {
    }

    public interface ICalculateNetCostStrategy : ICalculateCostStrategy, ISingletonDependency
    {
    }
}