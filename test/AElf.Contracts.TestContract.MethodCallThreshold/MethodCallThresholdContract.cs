using Acs5;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.MethodCallThreshold
{
    public class MethodCallThresholdContract : MethodCallThresholdContractContainer.MethodCallThresholdContractBase
    {
        public override Empty SetMethodCallingThreshold(SetMethodCallingThresholdInput input)
        {
            AssertPerformedByContractOwner();
            State.MethodCallingThresholds[input.Method] = new MethodCallingThreshold
            {
                SymbolToAmount = {input.SymbolToAmount}
            };
            return new Empty();
        }

        public override MethodCallingThreshold GetMethodCallingThreshold(StringValue input)
        {
            return State.MethodCallingThresholds[input.Value];
        }

        public override Empty SendForFun(Empty input)
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