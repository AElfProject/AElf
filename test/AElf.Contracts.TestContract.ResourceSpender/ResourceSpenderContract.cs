using Acs8;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.ResourceSpender
{
    public class ResourceSpenderContract : ResourceSpenderContractContainer.ResourceSpenderContractBase
    {
        public override Empty SetResourceConsumptionAmount(SetResourceConsumptionAmountInput input)
        {
            if (State.ACS0Contract.Value == null)
            {
                State.ACS0Contract.Value = Context.GetZeroSmartContractAddress();
            }

            var contractOwner = State.ACS0Contract.GetContractOwner.Call(Context.Self);
            Assert(contractOwner == Context.Sender, "Only contract owner can set resource consumption amount.");
            
            State.ResourceConsumptionAmounts[input.Method] = new ResourceConsumptionAmount
            {
                SymbolToAmount = {input.SymbolToAmount}
            };
            return new Empty();
        }

        public override ResourceConsumptionAmount GetResourceConsumptionAmount(StringValue input)
        {
            var amount = State.ResourceConsumptionAmounts[input.Value];
            if (amount != null)
            {
                return amount;
            }

            // Default result.
            return new ResourceConsumptionAmount
            {
                SymbolToAmount = {{"NET", 10L}, {"MEM", 10L}}
            };
        }

        public override Empty SendForFun(Empty input)
        {
            return new Empty();
        }
    }
}