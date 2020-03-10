using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public class MockFeeCalculateCoefficientProvider : ICoefficientsProvider, ISingletonDependency
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

        public MockFeeCalculateCoefficientProvider()
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

        public Task<List<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            return Task.FromResult(_coefficientsDicCache[tokenType]);
        }

        public Task SetAllCoefficientsAsync(Hash blockHash, AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            return Task.CompletedTask;
        }
    }
}