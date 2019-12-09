using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
namespace AElf.Kernel.TransactionPool.Application
{
    class CalculateStrategyProvider : ICalculateStrategyProvider
    {
        private Dictionary<FeeTypeEnum, ICalculateCostStrategy> DefaultCalculatorDic { get; set; }

        public CalculateStrategyProvider(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            DefaultCalculatorDic = new Dictionary<FeeTypeEnum, ICalculateCostStrategy>
            {
                [FeeTypeEnum.Cpu] = new CpuCalculateCostStrategy(tokenStTokenContractReaderFactory, blockchainService,
                    chainBlockLinkService),
                [FeeTypeEnum.Sto] = new StoCalculateCostStrategy(tokenStTokenContractReaderFactory, blockchainService,
                    chainBlockLinkService),
                [FeeTypeEnum.Net] = new NetCalculateCostStrategy(tokenStTokenContractReaderFactory, blockchainService,
                    chainBlockLinkService),
                [FeeTypeEnum.Ram] = new RamCalculateCostStrategy(tokenStTokenContractReaderFactory, blockchainService,
                    chainBlockLinkService),
                [FeeTypeEnum.Tx] = new TxCalculateCostStrategy(tokenStTokenContractReaderFactory, blockchainService,
                    chainBlockLinkService)
            };
        }

        public ICalculateCostStrategy GetCalculateStrategyByFeeType(int type)
        {
            var typeEnum = (FeeTypeEnum)type;
            return DefaultCalculatorDic.TryGetValue(typeEnum, out var strategy) ? strategy : null;
        }
        public ICalculateCostStrategy GetCpuCalculateStrategy()
        {
            return DefaultCalculatorDic[FeeTypeEnum.Cpu];
        }

        public ICalculateCostStrategy GetStoCalculateStrategy()
        {
            return DefaultCalculatorDic[FeeTypeEnum.Sto];
        }

        public ICalculateCostStrategy GetRamCalculateStrategy()
        {
            return DefaultCalculatorDic[FeeTypeEnum.Ram];
        }

        public ICalculateCostStrategy GetNetCalculateStrategy()
        {
            return DefaultCalculatorDic[FeeTypeEnum.Net];
        }

        public ICalculateCostStrategy GetTxCalculateStrategy()
        {
            return DefaultCalculatorDic[FeeTypeEnum.Tx];
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            foreach (var strategy in DefaultCalculatorDic)
            {
                strategy.Value.RemoveForkCache(blockIndexes);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            foreach (var strategy in DefaultCalculatorDic)
            {
                strategy.Value.SetIrreversedCache(blockIndexes);
            }
        }
    }
}