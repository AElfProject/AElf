using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;

namespace AElf.Kernel.FeeCalculation.Extensions
{
    public static class AllCalculateFeeCoefficientsExtensions
    {
        internal static Dictionary<string, CalculateFunction> ToCalculateFunctionDictionary(
            this AllCalculateFeeCoefficients allCalculateFeeCoefficients)
        {
            return allCalculateFeeCoefficients.Value.ToDictionary(c => ((FeeTypeEnum) c.FeeTokenType).ToString().ToUpper(),
                c => c.ToCalculateFunction());
        }
    }
}