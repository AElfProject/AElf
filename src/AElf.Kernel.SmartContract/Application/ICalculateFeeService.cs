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

    public interface ICalculateAlgorithmContext
    {
        int CalculateFeeTypeEnum { get; set; }
        BlockIndex BlockIndex { get; set; }
    }

    public interface ICalculateAlgorithmService
    {
        ICalculateAlgorithmContext CalculateAlgorithmContext { get; }
        ICalculateAlgorithmService AddDefaultAlgorithm(int pieceKey, ICalculateWay func);
        void AddAlgorithmByBlock(BlockIndex blockIndex, IList<ICalculateWay> funcList);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
        Task<long> CalculateAsync(int count);
    }


    public interface ICalculateWay
    {
        int PieceKey { get; set; }
        long GetCost(int initValue);
        void InitParameter(IDictionary<string, int> param);
        IDictionary<string, int> GetParameterDic();
        int FunctionTypeEnum { get; }
    }
}