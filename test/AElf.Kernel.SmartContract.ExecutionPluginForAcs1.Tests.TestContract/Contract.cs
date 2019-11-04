using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests.TestContract
{
    public class Contract : ContractContainer.ContractBase
    {
        public override Empty DummyMethod(Empty input)
        {
            // Do nothing
            return new Empty();
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            State.MethodFees[input.MethodName] = input;
            return new Empty();
        }

        public override MethodFees GetMethodFee(StringValue input)
        {
            return State.MethodFees[input.Value];
        }
    }
}