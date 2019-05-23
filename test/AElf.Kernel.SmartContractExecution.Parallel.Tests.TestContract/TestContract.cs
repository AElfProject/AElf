using Acs2;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel.Tests.TestContract
{
    public class TestContract : TestContractContainer.TestContractBase
    {
        public override ResourceInfo GetResourceInfo(Transaction input)
        {
            // Just echo input params
            return ResourceInfo.Parser.ParseFrom(input.Params);
        }
    }
}