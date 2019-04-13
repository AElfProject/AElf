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

        public override Empty SetMethodFee(SetMethodFeeInput input)
        {
            State.MethodFees[input.Method] = new TokenAmount()
            {
                Symbol = input.Symbol,
                Amount = input.Amount
            };
            return new Empty();
        }

        public override TokenAmount GetMethodFee(MethodName input)
        {
            return State.MethodFees[input.Name];
        }
    }
}