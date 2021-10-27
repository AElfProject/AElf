using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.TestContract
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
            State.TransactionFees[input.MethodName] = input;
            return new Empty();
        }

        public override MethodFees GetMethodFee(StringValue input)
        {
            return State.TransactionFees[input.Value];
        }
    }
}