using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.TransactionPool.Application
{
    #region concrete strategys

    abstract class CalculateCostStrategyBase
    {
        protected ICalculateAlgorithm CalculateAlgorithm { get; set; }

        public async Task<long> GetCostAsync(IChainContext chainContext, int cost)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            return await CalculateAlgorithm.CalculateAsync(cost);
        }

        public async Task ModifyAlgorithmAsync(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            IDictionary<string, int> param)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            CalculateAlgorithm.CalculateAlgorithmContext.BlockIndex = blockIndex;
            await CalculateAlgorithm.UpdateAsync(pieceKey, param);
        }

        public async Task ChangeAlgorithmPieceKeyAsync(IChainContext chainContext, BlockIndex blockIndex,
            int oldPieceKey, int newPieceKey)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            CalculateAlgorithm.CalculateAlgorithmContext.BlockIndex = blockIndex;
            await CalculateAlgorithm.ChangePieceKeyAsync(oldPieceKey, newPieceKey);
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            CalculateAlgorithm.RemoveForkCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            CalculateAlgorithm.SetIrreversedCache(blockIndexes);
        }
    }

    class CpuCalculateCostStrategy : CalculateCostStrategyBase, ICalculateCpuCostStrategy
    {
        public CpuCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(10, new LinerCalculateWay // used for unit test
                    {
                        Numerator = 1,
                        Denominator = 8,
                        ConstantValue = 10000
                    }).AddDefaultAlgorithm(100, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 4
                    }).AddDefaultAlgorithm(int.MaxValue, new PowerCalculateWay
                    {
                        Power = 2,
                        ChangeSpanBase = 4,
                        Weight = 250,
                        WeightBase = 40,
                        Numerator = 1,
                        Denominator = 4,
                        Precision = 100000000L
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Cpu;
        }
    }

    class StoCalculateCostStrategy : CalculateCostStrategyBase, ICalculateStoCostStrategy
    {
        public StoCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(1000000, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 64,
                        ConstantValue = 10000
                    }).AddDefaultAlgorithm(int.MaxValue, new PowerCalculateWay
                    {
                        Power = 2,
                        ChangeSpanBase = 100,
                        Weight = 250,
                        WeightBase = 500,
                        Numerator = 1,
                        Denominator = 64,
                        Precision = 100000000L
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Sto;
        }
    }

    class RamCalculateCostStrategy : CalculateCostStrategyBase, ICalculateRamCostStrategy
    {
        public RamCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(10, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 8,
                        ConstantValue = 10000
                    }).AddDefaultAlgorithm(100, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 4
                    }).AddDefaultAlgorithm(int.MaxValue, new PowerCalculateWay
                    {
                        Power = 2,
                        ChangeSpanBase = 2,
                        Weight = 250,
                        Numerator = 1,
                        Denominator = 4,
                        WeightBase = 40,
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Ram;
        }
    }

    class NetCalculateCostStrategy : CalculateCostStrategyBase, ICalculateNetCostStrategy
    {
        public NetCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(1000000, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 64,
                        ConstantValue = 10000
                    }).AddDefaultAlgorithm(int.MaxValue, new PowerCalculateWay
                    {
                        Power = 2,
                        ChangeSpanBase = 100,
                        Weight = 250,
                        WeightBase = 500,
                        Numerator = 1,
                        Denominator = 64,
                        Precision = 100000000L
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Net;
        }
    }

    class TxCalculateCostStrategy : CalculateCostStrategyBase, ICalculateTxCostStrategy
    {
        public TxCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(1000000, new LinerCalculateWay
                    {
                        Numerator = 1,
                        Denominator = 16 * 50,
                        ConstantValue = 10000
                    }).AddDefaultAlgorithm(int.MaxValue, new PowerCalculateWay
                    {
                        Power = 2,
                        ChangeSpanBase = 100,
                        Weight = 1,
                        WeightBase = 1,
                        Numerator = 1,
                        Denominator = 16 * 50,
                        Precision = 100000000L
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    #endregion
}