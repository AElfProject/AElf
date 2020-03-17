using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.TestBase;

namespace AElf.Kernel.FeeCalculation
{
    public class TransactionFeeTestBase : AElfIntegratedTest<KernelTransactionFeeTestAElfModule>
    {
        protected string GetBlockExecutedDataKey()
        {
            var list = new List<string> {KernelConstants.BlockExecutedDataKey, nameof(AllCalculateFeeCoefficients)};
            return string.Join("/", list);
        }
    }
}