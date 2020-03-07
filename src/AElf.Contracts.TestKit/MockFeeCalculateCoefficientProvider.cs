using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.FeeCalculation.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public class MockFeeCalculateCoefficientProvider : ICoefficientsCacheProvider, ISingletonDependency
    {
        private enum FeeTypeEnum {
            READ = 0,
            STORAGE = 1,
            WRITE = 2,
            TRAFFIC = 3,
            TX = 4,
        }
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        
        public MockFeeCalculateCoefficientProvider()
        {
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            var txCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 800, 0, 10000, 100000000 },
                new[] {int.MaxValue, 1, 1, 800, 2, 1, 10000}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.TX] = txCoefficient;
            var readCoefficient = new List<int[]>
            {
                new[] {10, 1, 1, 8, 0, 1000, 100000000 }, new[] {100, 1, 1, 4},
                new[] {int.MaxValue, 2, 25, 16, 1, 1, 4}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.READ] = readCoefficient;
            var storageCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 4, 0, 1000, 100000000 },
                new[] {int.MaxValue, 2, 1, 20000, 1, 1, 64}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.STORAGE] = storageCoefficient;
            var writeCoefficient = new List<int[]>
            {
                new[] {10, 1, 1, 8, 0, 10000, 100000000 },
                new[] {100, 1, 1, 4},
                new[] {int.MaxValue, 1, 1, 4, 2, 25, 16}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.WRITE] = writeCoefficient;
            var trafficCoefficient = new List<int[]>
            {
                new[] {1000000, 1, 1, 64, 0, 10000, 100000000 },
                new[] {int.MaxValue, 1, 1, 64, 2, 1, 20000}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.TRAFFIC] = trafficCoefficient;
        }

        public Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            return Task.FromResult(_coefficientsDicCache[tokenType]);
        }

        public void UpdateLatestModifiedHeight(long height)
        {
        }

        public bool GetUpdateNotification(int tokenType)
        {
            return false;
        }

        public Task SyncCacheAsync(IChainContext chainContext)
        {
            return Task.CompletedTask;
        }
    }
}