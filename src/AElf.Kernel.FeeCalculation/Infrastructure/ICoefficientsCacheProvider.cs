using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface ICoefficientsProvider
    {
        Task<List<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext);
        Task SetAllCoefficientsAsync(Hash blockHash, AllCalculateFeeCoefficients allCalculateFeeCoefficients);
    }

    public class CoefficientsProvider : BlockExecutedDataProvider, ICoefficientsProvider, ISingletonDependency
    {
        private const string BlockExecutedDataName = nameof(AllCalculateFeeCoefficients);
        
        private readonly ICachedBlockchainExecutedDataService<AllCalculateFeeCoefficients>
            _cachedBlockchainExecutedDataService;

        public CoefficientsProvider(
            ICachedBlockchainExecutedDataService<AllCalculateFeeCoefficients> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }

        public Task<List<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            var allCalculateFeeCoefficients =
                _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, GetBlockExecutedDataKey());
            var targetTokeData =
                allCalculateFeeCoefficients.Value.SingleOrDefault(x => x.FeeTokenType == tokenType);
            if (targetTokeData == null) return Task.FromResult(new List<int[]>());
            var coefficientsArray = targetTokeData.PieceCoefficientsList.AsEnumerable()
                .Select(x =>  x.Value.ToArray()).ToList();
            return Task.FromResult(coefficientsArray);
        }

        public async Task SetAllCoefficientsAsync(Hash blockHash, AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, GetBlockExecutedDataKey(),
                allCalculateFeeCoefficients);
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}