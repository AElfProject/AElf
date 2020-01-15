using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;

namespace AElf.Contract.TestContract
{
    public class TestCalculateTxStrategy : CalculateCostStrategyBase, ICalculateTxCostStrategy
    {
        public TestCalculateTxStrategy()
        {
            var functionProvider = new CalculateFunctionCacheProvider();
            var pieceWiseFuncCache = new Dictionary<int, ICalculateWay>
            {
                [1000000] = new LinerCalculateWay {Numerator = 1, Denominator = 800, ConstantValue = 10000},
                [int.MaxValue] = new PowerCalculateWay
                {
                    Power = 2,
                    ChangeSpanBase = 100,
                    Weight = 1,
                    WeightBase = 1,
                    Numerator = 1,
                    Denominator = 800,
                    Precision = 100000000L
                }
            };
            functionProvider.SetPieceWiseFunctionToNormalCache(pieceWiseFuncCache);
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateReadStrategy : CalculateCostStrategyBase, ICalculateReadCostStrategy
    {
        public TestCalculateReadStrategy()
        {
            var functionProvider = new CalculateFunctionCacheProvider();
            var pieceWiseFuncCache = new Dictionary<int, ICalculateWay>
            {
                [10] = new LinerCalculateWay {Numerator = 1, Denominator = 8, ConstantValue = 10000},
                [100] = new LinerCalculateWay {Numerator = 1, Denominator = 4},
                [int.MaxValue] = new PowerCalculateWay
                {
                    Power = 2,
                    ChangeSpanBase = 4,
                    Weight = 250,
                    WeightBase = 40,
                    Numerator = 1,
                    Denominator = 4,
                    Precision = 100000000L
                }
            };
            functionProvider.SetPieceWiseFunctionToNormalCache(pieceWiseFuncCache);
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateStorageStrategy : CalculateCostStrategyBase, ICalculateStorageCostStrategy
    {
        public TestCalculateStorageStrategy()
        {
            var functionProvider = new CalculateFunctionCacheProvider();
            var pieceWiseFuncCache = new Dictionary<int, ICalculateWay>
            {
                [1000000] = new LinerCalculateWay {Numerator = 1, Denominator = 64, ConstantValue = 10000},
                [int.MaxValue] = new PowerCalculateWay
                {
                    Power = 2,
                    ChangeSpanBase = 100,
                    Weight = 250,
                    WeightBase = 500,
                    Numerator = 1,
                    Denominator = 64,
                    Precision = 100000000L
                }
            };
            functionProvider.SetPieceWiseFunctionToNormalCache(pieceWiseFuncCache);
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateWriteStrategy : CalculateCostStrategyBase, ICalculateWriteCostStrategy
    {
        public TestCalculateWriteStrategy()
        {
            var functionProvider = new CalculateFunctionCacheProvider();
            var pieceWiseFuncCache = new Dictionary<int, ICalculateWay>
            {
                [10] = new LinerCalculateWay {Numerator = 1, Denominator = 8, ConstantValue = 10000},
                [100] = new LinerCalculateWay {Numerator = 1, Denominator = 4},
                [int.MaxValue] = new PowerCalculateWay
                {
                    Power = 2,
                    ChangeSpanBase = 2,
                    Weight = 250,
                    Numerator = 1,
                    Denominator = 4,
                    WeightBase = 40,
                }
            };
            functionProvider.SetPieceWiseFunctionToNormalCache(pieceWiseFuncCache);
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateTrafficStrategy : CalculateCostStrategyBase, ICalculateTrafficCostStrategy
    {
        public TestCalculateTrafficStrategy()
        {
            var functionProvider = new CalculateFunctionCacheProvider();
            var pieceWiseFuncCache = new Dictionary<int, ICalculateWay>
            {
                [1000000] = new LinerCalculateWay {Numerator = 1, Denominator = 64, ConstantValue = 10000},
                [int.MaxValue] = new PowerCalculateWay
                {
                    Power = 2,
                    ChangeSpanBase = 100,
                    Weight = 250,
                    WeightBase = 500,
                    Numerator = 1,
                    Denominator = 64,
                    Precision = 100000000L
                }
            };
            functionProvider.SetPieceWiseFunctionToNormalCache(pieceWiseFuncCache);
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }
}