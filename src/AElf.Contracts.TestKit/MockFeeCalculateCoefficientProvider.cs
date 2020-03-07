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
                new[] {0, 1000000, 1, 800, 10000},
                new[] {1, int.MaxValue, 1, 800, 2, 100, 1, 1}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.TX] = txCoefficient;
            var readCoefficient = new List<int[]>
            {
                new[] {0, 10, 1, 8, 1000}, new[] {0, 100, 1, 4, 0},
                new[] {1, int.MaxValue, 1, 4, 2, 5, 250, 40}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.READ] = readCoefficient;
            var storageCoefficient = new List<int[]>
            {
                new[] {0, 1000000, 1, 4, 1000},
                new[] {1, int.MaxValue, 1, 64, 2, 100, 250, 500}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.STORAGE] = storageCoefficient;
            var writeCoefficient = new List<int[]>
            {
                new[] {0, 10, 1, 8, 10000},
                new[] {0, 100, 1, 4, 0},
                new[] {1, int.MaxValue, 1, 4, 2, 2, 250, 40}
            };
            _coefficientsDicCache[(int) FeeTypeEnum.WRITE] = writeCoefficient;
            var trafficCoefficient = new List<int[]>
            {
                new[] {0, 1000000, 1, 64, 10000},
                new[] {1, int.MaxValue, 1, 64, 2, 100, 250, 500}
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