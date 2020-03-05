using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class MockCoefficientProvider : ICoefficientsCacheProvider, ISingletonDependency
    {
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        public MockCoefficientProvider()
        {
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            var txCoefficient = new List<int[]>
            {
                new []  { 1000000,1,800,10000 },
                new []  {int.MaxValue, 1, 800, 2, 100, 1, 1 }
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Tx] = txCoefficient;
            var readCoefficient = new List<int[]>
            {
                new[] {10, 1, 8, 1000}, new[] {100, 1, 4, 0},
                new[] {int.MaxValue, 1, 4, 2, 5, 250, 40}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Read] = readCoefficient;
            var storageCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 4, 1000},
                new[] {int.MaxValue, 1, 64, 2, 100, 250, 500}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Storage] = storageCoefficient;
            var writeCoefficient = new List<int[]>
            {
                new[] {10, 1, 8, 10000},
                new[] {100, 1, 4, 0},
                new[] {int.MaxValue, 1, 4, 2, 2, 250, 40}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Write] = writeCoefficient;
            var trafficCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 64, 10000},
                new[] {int.MaxValue, 1, 64, 2, 100, 250, 500}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.Traffic] = trafficCoefficient;
        }
        
        public Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            return Task.FromResult(_coefficientsDicCache[tokenType]);
        }
        public void SetModifyHeight(int tokenType)
        {
        }
        public Task SyncCacheAsync(IChainContext chainContext)
        {
            return Task.CompletedTask;
        }
    }
}