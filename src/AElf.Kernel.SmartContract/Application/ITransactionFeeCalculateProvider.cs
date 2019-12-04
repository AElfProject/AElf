using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{ 
    public enum FeeType
    {
        Tx = 0,
        Cpu,
        Sto,
        Ram,
        Net
    }

    public interface ICalculateFeeService : ISingletonDependency
    {
        Task<long> CalculateFee(IChainContext chainContext, FeeType feeType, int cost);

        Task AddFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey,
            CalculateFunctionType funcType, IDictionary<string, string> param);

        Task DeleteFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey);

        Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey,
            CalculateFunctionType funcType,
            IDictionary<string, string> para);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public interface ICalculateStrategyProvider : ISingletonDependency
    {
        ICalculateCostStrategy GetCalculateStrategy(FeeType feeType);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public enum AlgorithmOpCode
    {
        AddFunc,
        DeleteFunc,
        UpdateFunc
    }

    public interface ICalculateCostStrategy
    {
        Task<long> GetCost(IChainContext context, int cost);

        Task UpdateAlgorithm(IChainContext chainContext, BlockIndex blockIndex, AlgorithmOpCode opCode, int pieceKey,
            CalculateFunctionType funcType = CalculateFunctionType.Default,
            IDictionary<string, string> param = null);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public interface ICalculateAlgorithmContext
    {
        FeeType CalculateFeeType { get; set; }
        IChainContext ChainContext { get; set; }
        BlockIndex BlockIndex { get; set; }
    }
    public interface ICalculateAlgorithm
    {
        ICalculateAlgorithmContext CalculateAlgorithmContext { get;}
        ICalculateAlgorithm AddDefaultAlgorithm(int limit, ICalculateWay func);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
        Task<long> Calculate(int count);
        Task Delete(int pieceKey);
        Task Update(int pieceKey, CalculateFunctionType funcType, IDictionary<string, string> parameters);
        Task AddByParam(int pieceKey, CalculateFunctionType funcType, IDictionary<string, string> parameters);
    }

    public enum CalculateFunctionType
    {
        Default = 0,
        Constant,
        Liner,
        Power,
        Ln,
        Bancor
    }

    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool InitParameter(IDictionary<string, string> param);
        IDictionary<string, string> GetParameterDic();
        CalculateFunctionType FunctionType { get;}

    }

    class DefaultCalculateFeeService : ICalculateFeeService
    {
        //private readonly ICalculateStrategyProvider _calculateStrategyProvider;

        public DefaultCalculateFeeService()
        {
            //_calculateStrategyProvider = calculateStrategyProvider;
        }

        public async Task<long> CalculateFee(IChainContext chainContext, FeeType feeType, int cost)
        {
            return 0;
        }

        public async Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey,
            CalculateFunctionType funcType,
            IDictionary<string, string> param)
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

        public async Task DeleteFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey)
        {
            throw new System.NotImplementedException();
        }

        public async Task AddFeeCal(IChainContext chainContext, BlockIndex blockIndex, FeeType feeType, int pieceKey,
            CalculateFunctionType funcType,
            IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }
    }
}