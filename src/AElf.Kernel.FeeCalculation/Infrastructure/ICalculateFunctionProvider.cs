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
        Task AddCalculateFunctions(IBlockIndex blockIndex, Dictionary<string, CalculateFunction> calculateFunctionDictionary);
        Dictionary<string, CalculateFunction> GetCalculateFunctions(IChainContext chainContext);
    }

    public class CalculateFunctionProvider : BlockExecutedDataBaseProvider<Dictionary<string, CalculateFunction>>, ICalculateFunctionProvider, ISingletonDependency
    {
        public CalculateFunctionProvider(ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
            calculateFunctionExecutedDataService) : base(calculateFunctionExecutedDataService)
        {
        }

        public async Task AddCalculateFunctions(IBlockIndex blockIndex,
            Dictionary<string, CalculateFunction> calculateFunctionDictionary)
        {
            await AddBlockExecutedDataAsync(blockIndex, calculateFunctionDictionary);
        }

        public Dictionary<string, CalculateFunction> GetCalculateFunctions(IChainContext chainContext)
        {
            return GetBlockExecutedData(chainContext);
        }

        private const string BlockExecutedDataName = nameof(AllCalculateFeeCoefficients);

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }
    }
}