using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{ 
    public enum FeeTypeEnum
    {
        Tx = 1,
        Cpu,
        Sto,
        Ram,
        Net
    }
    public enum CalculateFunctionTypeEnum
    {
        Default = 1,
        Constant,
        Liner,
        Power,
        Ln,
        Bancor
    }
    public enum AlgorithmOpCodeEnum
    {
        AddFunc = 1,
        DeleteFunc,
        UpdateFunc
    }

    public interface ICalculateFeeService : ITransientDependency
    {
        ICalculateCostStrategy CalculateCostStrategy { get; set; }
        Task<long> CalculateFee(IChainContext chainContext, int cost);

        Task AddFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum, IDictionary<string, string> param);

        Task DeleteFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey);

        Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> para);
    }

    public interface ICalculateStrategyProvider : ISingletonDependency
    {
        ICalculateCostStrategy GetCalculateStrategyByFeeType(FeeTypeEnum typeEnum);
        ICalculateCostStrategy GetCpuCalculateStrategy();
        ICalculateCostStrategy GetStoCalculateStrategy();
        ICalculateCostStrategy GetRamCalculateStrategy();
        ICalculateCostStrategy GetNetCalculateStrategy();
        ICalculateCostStrategy GetTxCalculateStrategy();
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public interface ICalculateCostStrategy
    {
        Task<long> GetCost(IChainContext context, int cost);

        Task UpdateAlgorithm(IChainContext chainContext, BlockIndex blockIndex, AlgorithmOpCodeEnum opCodeEnum, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum = CalculateFunctionTypeEnum.Default,
            IDictionary<string, string> param = null);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public interface ICalculateAlgorithmContext
    {
        FeeTypeEnum CalculateFeeTypeEnum { get; set; }
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
        Task Update(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum, IDictionary<string, string> parameters);
        Task AddByParam(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum, IDictionary<string, string> parameters);
    }

   

    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool InitParameter(IDictionary<string, string> param);
        IDictionary<string, string> GetParameterDic();
        CalculateFunctionTypeEnum FunctionTypeEnum { get;}

    }

    class DefaultCalculateFeeService : ICalculateFeeService
    {
        public ICalculateCostStrategy CalculateCostStrategy { get; set; }

        public async Task<long> CalculateFee(IChainContext chainContext, int cost)
        {
            return 0;
        }

        public async Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }
        public async Task DeleteFeeCal(IChainContext chainContext, BlockIndex blockIndex,int pieceKey)
        {
            throw new System.NotImplementedException();
        }

        public async Task AddFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> param)
        {
            throw new System.NotImplementedException();
        }
    }

    class DefaultCalculateStrategyProvider : ICalculateStrategyProvider
    {
        public ICalculateCostStrategy GetCalculateStrategyByFeeType(FeeTypeEnum typeEnum)
        {
            throw new System.NotImplementedException();
        }

        public ICalculateCostStrategy GetCpuCalculateStrategy()
        {
            throw new System.NotImplementedException();
        }

        public ICalculateCostStrategy GetStoCalculateStrategy()
        {
            throw new System.NotImplementedException();
        }

        public ICalculateCostStrategy GetRamCalculateStrategy()
        {
            throw new System.NotImplementedException();
        }

        public ICalculateCostStrategy GetNetCalculateStrategy()
        {
            throw new System.NotImplementedException();
        }

        public ICalculateCostStrategy GetTxCalculateStrategy()
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
}