using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class CalculateFunctionExecutedDataService :
        CachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
    {
        public ILogger<CalculateFunctionExecutedDataService> Logger { get; set; }

        public CalculateFunctionExecutedDataService(IBlockchainExecutedDataManager blockchainExecutedDataManager,
            IBlockchainExecutedDataCacheProvider<Dictionary<string, CalculateFunction>> blockchainExecutedDataCacheProvider) :
            base(blockchainExecutedDataManager, blockchainExecutedDataCacheProvider)
        {
            Logger = NullLogger<CalculateFunctionExecutedDataService>.Instance;
        }

        protected override Dictionary<string, CalculateFunction> Deserialize(ByteString byteString)
        {
            var allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
            allCalculateFeeCoefficients.MergeFrom(byteString);
            Logger.LogDebug($"Deserialize AllCalculateFeeCoefficients: {allCalculateFeeCoefficients}");
            return allCalculateFeeCoefficients.Value.ToDictionary(
                c => ((FeeTypeEnum) c.FeeTokenType).ToString().ToUpper(),
                c => c.ToCalculateFunction());
        }

        protected override ByteString Serialize(Dictionary<string, CalculateFunction> functionMap)
        {
            var allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
            foreach (var functionPair in functionMap)
            {
                allCalculateFeeCoefficients.Value.Add(new CalculateFeeCoefficients
                {
                    FeeTokenType = functionPair.Value.CalculateFeeCoefficients.FeeTokenType,
                    PieceCoefficientsList = {functionPair.Value.CalculateFeeCoefficients.PieceCoefficientsList}
                });
            }

            Logger.LogDebug($"Serialize AllCalculateFeeCoefficients: {allCalculateFeeCoefficients}");
            return allCalculateFeeCoefficients.ToByteString();
        }
    }
}