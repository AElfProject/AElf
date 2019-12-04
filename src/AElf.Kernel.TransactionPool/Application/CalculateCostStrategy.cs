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

        public async Task UpdateAlgorithm(IChainContext chainContext, BlockIndex blockIndex, AlgorithmOpCode opCode,
            int pieceKey,
            CalculateFunctionType funcType,
            IDictionary<string, string> param)
        {
            CalculateAlgorithm.CalculateAlgorithmContext.ChainContext = chainContext;
            CalculateAlgorithm.CalculateAlgorithmContext.BlockIndex = blockIndex;
            switch (opCode)
            {
                case AlgorithmOpCode.AddFunc:
                    await CalculateAlgorithm.AddByParam(pieceKey, funcType, param);
                    break;
                case AlgorithmOpCode.DeleteFunc:
                    await CalculateAlgorithm.Delete(pieceKey);
                    break;
                case AlgorithmOpCode.UpdateFunc:
                    await CalculateAlgorithm.Update(pieceKey, funcType, param);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
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
                        ChangeSpanBase = 4,    
                        Weight = 250,
                        WeightBase = 40,
                        Numerator = 1,
                        Denominator = 4,
                        Precision = 100000000L
                    });
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeType = FeeType.Cpu;
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeType = FeeType.Sto;
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeType = FeeType.Ram;
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeType = FeeType.Net;
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
            CalculateAlgorithm.CalculateAlgorithmContext.CalculateFeeType = FeeType.Tx;
        }
    }

    #endregion
}