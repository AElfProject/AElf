using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;

namespace AElf.ContractTestKit
{
    /// <summary>
    /// TODO: Dup this code here to resolve refs conflicts. Remove after figuring out how to de-couple refs.
    /// </summary>
    internal static class AllCalculateFeeCoefficientsExtensions
    {
        public static Dictionary<string, CalculateFunction> ToCalculateFunctionDictionary(
            this AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            return allCalculateFeeCoefficients.Value.ToDictionary(c => ((FeeTypeEnum) c.FeeTokenType).ToString().ToUpper(),
                c => c.ToCalculateFunction());
        }
    }
}