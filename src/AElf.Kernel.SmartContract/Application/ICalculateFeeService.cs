using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateCostStrategy
    {
        Task<long> GetCostAsync(IChainContext context, int cost);

        Task ModifyAlgorithmAsync(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            IDictionary<string, int> param);

        Task ChangeAlgorithmPieceKeyAsync(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey,
            int newPieceKey);

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
        IChainContext ChainContext { get; set; }
        BlockIndex BlockIndex { get; set; }
    }

    public interface ICalculateAlgorithm
    {
        ICalculateAlgorithmContext CalculateAlgorithmContext { get; }
        ICalculateAlgorithm AddDefaultAlgorithm(int limit, ICalculateWay func);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
        Task<long> CalculateAsync(int count);
        Task UpdateAsync(int pieceKey, IDictionary<string, int> parameters);
        Task ChangePieceKeyAsync(int oldPieceKey, int newPieceKey);
    }


    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool TryInitParameter(IDictionary<string, int> param);
        IDictionary<string, int> GetParameterDic();
        int FunctionTypeEnum { get; }
    }
}