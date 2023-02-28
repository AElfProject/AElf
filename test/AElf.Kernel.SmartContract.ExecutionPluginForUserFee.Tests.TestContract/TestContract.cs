using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForUserFee.Tests.TestContract;

public class TestContract : TestContractContainer.TestContractBase
{
    public override Empty TestMethod(Empty input)
    {
        // Do nothing
        return new Empty();
    }

}