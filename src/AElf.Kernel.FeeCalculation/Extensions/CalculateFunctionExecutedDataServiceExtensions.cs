using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Extensions
{
    public static class CalculateFunctionExecutedDataServiceExtensions
    {
        public static async Task AddBlockExecutedDataAsync(
            this ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
                cachedBlockchainExecutedDataGettingService, Hash blockHash, string key,
            AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            await cachedBlockchainExecutedDataGettingService.AddBlockExecutedDataAsync(blockHash, key,
                allCalculateFeeCoefficients.ToCalculateFunctionDictionary());
        }
    }
}