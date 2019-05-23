using Acs2;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel.Tests.TestContract
{
    public class TestContract : TestContractContainer.TestContractBase
    {
        public override ResourceInfo GetResourceInfo(Transaction input)
        {
            return new ResourceInfo
            {
                Reources = {input.GetHashCode()}
            };
        }
    }
}