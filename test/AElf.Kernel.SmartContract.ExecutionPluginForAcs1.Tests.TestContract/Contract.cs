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

        public override Empty SetMethodFee(TokenAmounts input)
        {
            State.MethodFees[input.Method] = input;
            return new Empty();
        }

        public override TokenAmounts GetMethodFee(MethodName input)
        {
            return State.MethodFees[input.Name];
        }
    }
}