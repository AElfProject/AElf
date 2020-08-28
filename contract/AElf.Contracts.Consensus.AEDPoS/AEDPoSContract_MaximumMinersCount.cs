using System;
using AElf.Standards.ACS1;
using AElf.Contracts.Election;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override Empty SetMaximumMinersCount(Int32Value input)
        {
            if (State.ElectionContract.Value == null)
            {
                State.ElectionContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            }

            Assert(input.Value > 0, "Invalid max miners count.");

            RequiredMaximumMinersCountControllerSet();
            Assert(Context.Sender == State.MaximumMinersCountController.Value.OwnerAddress,
                "No permission to set max miners count.");
            State.MaximumMinersCount.Value = input.Value;
            State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
            {
                MinersCount = input.Value
            });
            return new Empty();
        }

        private void RequiredMaximumMinersCountControllerSet()
        {
            if (State.MaximumMinersCountController.Value != null) return;
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                ContractAddress = State.ParliamentContract.Value
            };

            State.MaximumMinersCountController.Value = defaultAuthority;
        }

        public override Empty ChangeMaximumMinersCountController(AuthorityInfo input)
        {
            RequiredMaximumMinersCountControllerSet();
            AssertSenderAddressWith(State.MaximumMinersCountController.Value.OwnerAddress);
            var organizationExist = CheckOrganizationExist(input);
            Assert(organizationExist, "Invalid authority input.");

            State.MaximumMinersCountController.Value = input;
            return new Empty();
        }

        public override AuthorityInfo GetMaximumMinersCountController(Empty input)
        {
            RequiredMaximumMinersCountControllerSet();
            return State.MaximumMinersCountController.Value;
        }

        public override Int32Value GetMaximumMinersCount(Empty input)
        {
            if (State.BlockchainStartTimestamp.Value == null)
            {
                return new Int32Value {Value = AEDPoSContractConstants.SupposedMinersCount};
            }

            return new Int32Value
            {
                Value = Math.Min(AEDPoSContractConstants.SupposedMinersCount.Add(
                    (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                    .Div(State.MinerIncreaseInterval.Value).Mul(2)), State.MaximumMinersCount.Value)
            };
        }
    }
}