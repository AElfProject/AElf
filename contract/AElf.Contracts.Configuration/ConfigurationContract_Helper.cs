using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract
    {
        private void ValidateContractState(ContractReferenceState state, Hash contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }

        private Address GetOwnerAddress()
        {
            if (State.Owner.Value != null)
                return State.Owner.Value;
            ValidateContractState(State.ParliamentAuthContract,
                SmartContractConstants.ParliamentAuthContractSystemName);
            Address organizationAddress = State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty());
            State.Owner.Value = organizationAddress;

            return State.Owner.Value;
        }

        private void CheckOwnerAuthority()
        {
            var owner = GetOwnerAddress();
            Assert(owner.Equals(Context.Sender), "Not authorized to do this.");
        }
    }
}