using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Profit.Helpers
{
    public static class ProfitHelper
    {
        public static Hash GeneratePeriodVirtualAddressFromHash(Hash schemeId, long period)
        {
            return HashHelper.XorAndCompute(schemeId, HashHelper.ComputeFrom(period));
        }

        public static Address CalculatePeriodVirtualAddress(CSharpSmartContractContext context, Hash schemeId,
            long period)
        {
            return context.ConvertVirtualAddressToContractAddress(
                GeneratePeriodVirtualAddressFromHash(schemeId, period));
        }
    }
}