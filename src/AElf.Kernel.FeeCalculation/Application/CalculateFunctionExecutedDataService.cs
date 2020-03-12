using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class CalculateFunctionExecutedDataService :
        CachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
    {
        public CalculateFunctionExecutedDataService(IBlockchainExecutedDataManager blockchainExecutedDataManager) :
            base(blockchainExecutedDataManager)
        {
        }

        protected override Dictionary<string, CalculateFunction> Deserialize(ByteString byteString)
        {
            var allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
            allCalculateFeeCoefficients.MergeFrom(byteString);
            return allCalculateFeeCoefficients.Value.ToDictionary(c => ((FeeTypeEnum) c.FeeTokenType).ToString(),
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

            return allCalculateFeeCoefficients.ToByteString();
        }
    }
}