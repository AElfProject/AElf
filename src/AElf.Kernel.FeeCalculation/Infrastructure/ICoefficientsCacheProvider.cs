using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        public ILogger<CoefficientsProvider> Logger { get; set; }

        public CoefficientsProvider(
            ICachedBlockchainExecutedDataService<AllCalculateFeeCoefficients> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
            
            Logger = NullLogger<CoefficientsProvider>.Instance;
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
            foreach (var coefficients in coefficientsArray)
            {
                LogNewFunctionCoefficients(coefficients);
            }
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
        
        private void LogNewFunctionCoefficients(params int[] parameters)
        {
            var log = $"New function (Upper bound {parameters[0]}):\n";
            var currentIndex = 1;
            while (currentIndex < parameters.Length)
            {
                var power = parameters[currentIndex];
                var divisor = parameters[currentIndex + 1];
                var dividend = parameters[currentIndex + 2];
                log += $"{divisor} / {dividend} * x^{power} +";
                currentIndex += 3;
            }

            log = log.Substring(0, log.Length - 1);
            Logger.LogInformation(log);
        }
    }
}