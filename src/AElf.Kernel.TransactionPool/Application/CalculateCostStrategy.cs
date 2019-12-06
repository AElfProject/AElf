using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.TransactionPool.Application
{
    #region concrete strategys

    abstract class CalculateCostStrategyBase : ICalculateCostStrategy
    {
        protected ICalculateAlgorithm CalculateAlgorithm { get; set; }

        public async Task<long> GetCost(IChainContext chainContext, int cost)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            return await CalculateAlgorithm.Calculate(cost);
        }

        public async Task UpdateAlgorithm(IChainContext chainContext, BlockIndex blockIndex, AlgorithmOpCodeEnum opCodeEnum,
            int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> param)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            CalculateAlgorithm.CalculateAlgorithmContext.BlockIndex = blockIndex;
            switch (opCodeEnum)
            {
                case AlgorithmOpCodeEnum.AddFunc:
                    await CalculateAlgorithm.AddByParam(pieceKey, funcTypeEnum, param);
                    break;
                case AlgorithmOpCodeEnum.DeleteFunc:
                    await CalculateAlgorithm.Delete(pieceKey);
                    break;
                case AlgorithmOpCodeEnum.UpdateFunc:
                    await CalculateAlgorithm.Update(pieceKey, funcTypeEnum, param);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opCodeEnum), opCodeEnum, null);
            }
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

    class CpuCalculateCostStrategy : CalculateCostStrategyBase
    {
        public CpuCalculateCostStrategy(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            CalculateAlgorithm =
                new CalculateAlgorithm(tokenStTokenContractReaderFactory, blockchainService, chainBlockLinkService)
                    .AddDefaultAlgorithm(10, new LinerCalculateWay    // used for unit test
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = FeeTypeEnum.Cpu;
        }
    }

    class StoCalculateCostStrategy : CalculateCostStrategyBase
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = FeeTypeEnum.Sto;
        }
    }

    class RamCalculateCostStrategy : CalculateCostStrategyBase
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = FeeTypeEnum.Ram;
        }
    }

    class NetCalculateCostStrategy : CalculateCostStrategyBase
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = FeeTypeEnum.Net;
        }
    }

    class TxCalculateCostStrategy : CalculateCostStrategyBase
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeTypeEnum = FeeTypeEnum.Tx;
        }
    }

    #endregion
}