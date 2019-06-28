using Acs5;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests.TestContract
{
    public class Contract : ContractContainer.ContractBase
    {
        public override Empty SetMethodCallingThreshold(SetMethodCallingThresholdInput input)
        {
            AssertPerformedByContractOwner();

            State.MethodCallingThresholdFees[input.Method] = new MethodCallingThreshold
            {
                SymbolToAmount = { input.SymbolToAmount },
                ThresholdCheckType = input.ThresholdCheckType
            };

            return new Empty();
        }

        public override MethodCallingThreshold GetMethodCallingThreshold(StringValue input)
        {
            if(State.MethodCallingThresholdFees[input.Value] == null)
                return new MethodCallingThreshold();
            return new MethodCallingThreshold
            {
                SymbolToAmount =
                {
                    State.MethodCallingThresholdFees[input.Value].SymbolToAmount
                }
            };
        }

        public override Empty DummyMethod(Empty input)
        {
            return new Empty();
        }

        private void AssertPerformedByContractOwner()
        {
            if (State.Acs0Contract.Value == null)
            {
                State.Acs0Contract.Value = Context.GetZeroSmartContractAddress();
            }
            var contractInfo = State.Acs0Contract.GetContractInfo.Call(Context.Self);
            Assert(Context.Sender == contractInfo.Owner, "Only owner are permitted to call this method.");
        }
    }
}