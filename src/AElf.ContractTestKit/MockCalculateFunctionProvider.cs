using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.FeeCalculation.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestKit
{
    public class MockCalculateFunctionProvider : ICalculateFunctionProvider, ISingletonDependency
    {
        private enum FeeTypeEnum
        {
            Read = 0,
            Storage = 1,
            Write = 2,
            Traffic = 3,
            Tx = 4,
        }

        private readonly Dictionary<int, List<int[]>> _coefficientsDicCache;

        public MockCalculateFunctionProvider()
        {
            _coefficientsDicCache = new Dictionary<int, List<int[]>>();
            var txCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 800, 0, 10000, 100000000},
                new[] {int.MaxValue, 1, 1, 800, 2, 1, 10000}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Tx] = txCoefficient;
            var readCoefficient = new List<int[]>
            {
                new[] {10, 1, 1, 8, 0, 1000, 100000000}, new[] {100, 1, 1, 4},
                new[] {int.MaxValue, 2, 25, 16, 1, 1, 4}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Read] = readCoefficient;
            var storageCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 4, 0, 1000, 100000000},
                new[] {int.MaxValue, 2, 1, 20000, 1, 1, 64}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Storage] = storageCoefficient;
            var writeCoefficient = new List<int[]>
            {
                new[] {10, 1, 1, 8, 0, 10000, 100000000},
                new[] {100, 1, 1, 4},
                new[] {int.MaxValue, 1, 1, 4, 2, 25, 16}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Write] = writeCoefficient;
            var trafficCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 64, 0, 10000, 100000000},
                new[] {int.MaxValue, 1, 1, 64, 2, 1, 20000}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Traffic] = trafficCoefficient;
        }

        public Task AddCalculateFunctions(IBlockIndex blockIndex,
            Dictionary<string, CalculateFunction> calculateFunctionDictionary)
        {
            return Task.CompletedTask;
        }

        public Dictionary<string, CalculateFunction> GetCalculateFunctions(IChainContext chainContext)
        {
            var allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
            foreach (var coefficients in _coefficientsDicCache)
            {
                allCalculateFeeCoefficients.Value.Add(new CalculateFeeCoefficients
                {
                    FeeTokenType = coefficients.Key,
                    PieceCoefficientsList =
                    {
                        coefficients.Value.Select(v => new CalculateFeePieceCoefficients
                        {
                            Value = {v}
                        })
                    }
                });
            }
            return allCalculateFeeCoefficients.ToCalculateFunctionDictionary();
        }
    }
}