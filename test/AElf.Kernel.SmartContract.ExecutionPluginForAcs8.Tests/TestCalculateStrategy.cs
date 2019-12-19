using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    public class TestCalculateTxStrategy : CalculateCostStrategyBase, ICalculateTxCostStrategy
    {
        public TestCalculateTxStrategy()
        {
            var functionProvider = new CalculateFunctionProvider
            {
                PieceWiseFuncCache = new Dictionary<int, ICalculateWay>
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
                }
            };
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateCpuStrategy : CalculateCostStrategyBase, ICalculateCpuCostStrategy
    {
        public TestCalculateCpuStrategy()
        {
            var functionProvider = new CalculateFunctionProvider();
            functionProvider.PieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            functionProvider.PieceWiseFuncCache[10] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            };
            functionProvider.PieceWiseFuncCache[100] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            };
            functionProvider.PieceWiseFuncCache[int.MaxValue] = new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 4,
                Weight = 250,
                WeightBase = 40,
                Numerator = 1,
                Denominator = 4,
                Precision = 100000000L
            };

            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateStoStrategy : CalculateCostStrategyBase, ICalculateStoCostStrategy
    {
        public TestCalculateStoStrategy()
        {
            var functionProvider = new CalculateFunctionProvider();
            functionProvider.PieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            functionProvider.PieceWiseFuncCache[1000000] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            };
            functionProvider.PieceWiseFuncCache[int.MaxValue] = new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            };
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateRamStrategy : CalculateCostStrategyBase, ICalculateRamCostStrategy
    {
        public TestCalculateRamStrategy()
        {
            var functionProvider = new CalculateFunctionProvider();
            functionProvider.PieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            functionProvider.PieceWiseFuncCache[10] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            };
            functionProvider.PieceWiseFuncCache[100] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            };
            functionProvider.PieceWiseFuncCache[int.MaxValue] = new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 2,
                Weight = 250,
                Numerator = 1,
                Denominator = 4,
                WeightBase = 40,
            };
            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }

    public class TestCalculateNetStrategy : CalculateCostStrategyBase, ICalculateNetCostStrategy
    {
        public TestCalculateNetStrategy()
        {
            var functionProvider = new CalculateFunctionProvider();
            functionProvider.PieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            functionProvider.PieceWiseFuncCache[1000000] = new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            };
            functionProvider.PieceWiseFuncCache[int.MaxValue] = new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            };

            CalculateAlgorithmService =
                new CalculateAlgorithmService(null, null,
                    null, functionProvider);
            CalculateAlgorithmService.CalculateAlgorithmContext.CalculateFeeTypeEnum = (int) FeeTypeEnum.Tx;
        }
    }
}