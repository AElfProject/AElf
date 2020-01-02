using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract : ConfigurationContainer.ConfigurationBase
    {
        public override Empty SetBlockTransactionLimit(Int32Value input)
        {
            Assert(input.Value > 0, "Invalid input.");
            CheckOwnerAuthority();

            var oldValue = State.BlockTransactionLimit.Value;
            var newValue = input.Value;
            State.BlockTransactionLimit.Value = newValue;
            Context.Fire(new BlockTransactionLimitChanged
            {
                Old = oldValue,
                New = newValue
            });
            return new Empty();
        }

        public override Int32Value GetBlockTransactionLimit(Empty input)
        {
            return new Int32Value {Value = State.BlockTransactionLimit.Value};
        }

        public override Empty ChangeOwnerAddress(Address input)
        {
            CheckOwnerAuthority();
            State.Owner.Value = input;
            return new Empty();
        }

        public override Address GetOwnerAddress(Empty input)
        {
            var address = GetOwnerAddress();
            return address;
        }

        public override Empty RentResourceTokens(RentResourceTokensInput input)
        {
            CheckSenderIsCrossChainContract();
            State.RentedResourceTokenAmount[input.ChainId.Value] = input.ResourceTokenAmount;
            if (State.RemainResourceTokenAmount.Value != null)
                State.RemainResourceTokenAmount.Value -= input.ResourceTokenAmount;
            return new Empty();
        }

        public override Empty UpdateRentedResourceTokens(RentResourceTokensInput input)
        {
            CheckSenderIsCrossChainContract();
            Assert(State.RentedResourceTokenAmount[input.ChainId.Value] != null, "Rented resource amount not found.");
            var change = input.ResourceTokenAmount - State.RentedResourceTokenAmount[input.ChainId.Value];
            State.RentedResourceTokenAmount[input.ChainId.Value] = input.ResourceTokenAmount;
            if (State.RemainResourceTokenAmount.Value != null)
                State.RemainResourceTokenAmount.Value -= change;
            return new Empty();
        }

        public override Empty InitialTotalResourceTokens(ResourceTokenAmount input)
        {
            CheckSenderIsParliamentOrZeroContract();
            State.TotalResourceTokenAmount.Value = input;
            State.RemainResourceTokenAmount.Value = input;
            return new Empty();
        }

        public override Empty UpdateTotalResourceTokens(ResourceTokenAmount input)
        {
            CheckOwnerAuthority();
            var change = input - State.TotalResourceTokenAmount.Value;
            if (State.RemainResourceTokenAmount.Value == null)
                State.RemainResourceTokenAmount.Value = change;
            else
                State.RemainResourceTokenAmount.Value += change;
            State.TotalResourceTokenAmount.Value = input;
            return new Empty();
        }

        public override ResourceTokenAmount GetRentedResourceTokens(SInt32Value input)
        {
            return State.RentedResourceTokenAmount[input.Value] ?? new ResourceTokenAmount();
        }

        public override ResourceTokenAmount GetRemainResourceTokens(Empty input)
        {
            return State.RemainResourceTokenAmount.Value ?? new ResourceTokenAmount();
        }

        public override ResourceTokenAmount GetTotalResourceTokens(Empty input)
        {
            return State.TotalResourceTokenAmount.Value ?? new ResourceTokenAmount();
        }
    }
}