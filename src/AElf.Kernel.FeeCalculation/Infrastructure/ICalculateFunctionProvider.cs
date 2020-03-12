using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    /// <summary>
    /// To provide basic function for piece-wise function.
    /// </summary>
    public interface ICalculateFunctionProvider
    {
        Task AddCalculateFunctions(Hash blockHash, AllCalculateFeeCoefficients allCalculateFeeCoefficients);
        Dictionary<string, CalculateFunction> GetCalculateFunctions(IChainContext chainContext);
    }

    public class CalculateFunctionProvider : BlockExecutedDataProvider, ICalculateFunctionProvider, ISingletonDependency
    {
        private readonly ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
            _calculateFunctionExecutedDataService;

        public ILogger<CalculateFunctionProvider> Logger { get; set; }

        public CalculateFunctionProvider(ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
            calculateFunctionExecutedDataService)
        {
            _calculateFunctionExecutedDataService = calculateFunctionExecutedDataService;
            Logger = NullLogger<CalculateFunctionProvider>.Instance;
        }

        public async Task AddCalculateFunctions(Hash blockHash, AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            await _calculateFunctionExecutedDataService.AddBlockExecutedDataAsync(blockHash, GetBlockExecutedDataKey(),
                allCalculateFeeCoefficients);
        }

        public Dictionary<string, CalculateFunction> GetCalculateFunctions(IChainContext chainContext)
        {
            return _calculateFunctionExecutedDataService.GetBlockExecutedData(chainContext, GetBlockExecutedDataKey());
        }

        private const string BlockExecutedDataName = nameof(AllCalculateFeeCoefficients);

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}