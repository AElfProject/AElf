using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
//    public interface ICalculateFeeService : ISingletonDependency
//    {
//        ICalculateCostStrategy CalculateCostStrategy { get; set; }
//        Task<long> CalculateFee(IChainContext chainContext, int cost);
//        Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
//            IDictionary<string, string> para);
//        Task ModifyPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey);
//    }

//    public interface ICalculateStrategyProvider : ISingletonDependency
//    {
//        ICalculateCostStrategy GetCalculateStrategyByFeeType(int typeEnum);
//        ICalculateCostStrategy GetCpuCalculateStrategy();
//        ICalculateCostStrategy GetStoCalculateStrategy();
//        ICalculateCostStrategy GetRamCalculateStrategy();
//        ICalculateCostStrategy GetNetCalculateStrategy();
//        ICalculateCostStrategy GetTxCalculateStrategy();
//        void RemoveForkCache(List<BlockIndex> blockIndexes);
//        void SetIrreversedCache(List<BlockIndex> blockIndexes);
//    }
    public interface ICalculateCostStrategy
    {
        Task<long> GetCost(IChainContext context, int cost);

        Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            IDictionary<string, string> param);

        Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey,
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
        Task<long> Calculate(int count);
        Task Update(int pieceKey, IDictionary<string, string> parameters);
        Task ChangePieceKey(int oldPieceKey, int newPieceKey);
    }


    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool TryInitParameter(IDictionary<string, string> param);
        IDictionary<string, string> GetParameterDic();
        int FunctionTypeEnum { get; }
    }


    class DefaultCalculateTxCostStrategy : ICalculateTxCostStrategy
    {
        public Task<long> GetCost(IChainContext context, int cost)
        {
            throw new System.NotImplementedException();
        }

        public Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }

        public Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }
    class DefaultCalculateCpuCostStrategy : ICalculateCpuCostStrategy
    {
        public Task<long> GetCost(IChainContext context, int cost)
        {
            throw new System.NotImplementedException();
        }

        public Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }

        public Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }
    class DefaultCalculateRamCostStrategy : ICalculateRamCostStrategy
    {
        public Task<long> GetCost(IChainContext context, int cost)
        {
            throw new System.NotImplementedException();
        }

        public Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }

        public Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }
    class DefaultCalculateNetCostStrategy : ICalculateNetCostStrategy
    {
        public Task<long> GetCost(IChainContext context, int cost)
        {
            throw new System.NotImplementedException();
        }

        public Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }

        public Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }
    class DefaultCalculateStoCostStrategy : ICalculateStoCostStrategy
    {
        public Task<long> GetCost(IChainContext context, int cost)
        {
            throw new System.NotImplementedException();
        }

        public Task ModifyAlgorithm(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }

        public Task ChangeAlgorithmPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }

//    class DefaultCalculateFeeService : ICalculateFeeService
//    {
//        public ICalculateCostStrategy CalculateCostStrategy { get; set; }
//        public Task<long> CalculateFee(IChainContext chainContext, int cost)
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey, IDictionary<string, string> para)
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public Task ModifyPieceKey(IChainContext chainContext, BlockIndex blockIndex, int oldPieceKey, int newPieceKey)
//        {
//            throw new System.NotImplementedException();
//        }
//    }
//
//    class DefaultCalculateStrategyProvider : ICalculateStrategyProvider
//    {
//        public ICalculateCostStrategy GetCalculateStrategyByFeeType(int typeEnum)
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public ICalculateCostStrategy GetCpuCalculateStrategy()
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public ICalculateCostStrategy GetStoCalculateStrategy()
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public ICalculateCostStrategy GetRamCalculateStrategy()
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public ICalculateCostStrategy GetNetCalculateStrategy()
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public ICalculateCostStrategy GetTxCalculateStrategy()
//        {
//            throw new System.NotImplementedException();
//        }
//
//        public void RemoveForkCache(List<BlockIndex> blockIndexes)
//        {
//            return;
//        }
//
//        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
//        {
//            return;
//        }
//    }
}