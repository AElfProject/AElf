using AElf.Kernel.SmartContract.ExecutionPluginForUserFee.Tests.TestContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForSystemFee.Tests.TestContract;

public class TestContract : TestContractContainer.TestContractBase
{
    public override Empty TestMethod(Empty input)
    {
        // Do nothing
        return new Empty();
    }

}