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
            EnsureElectionContractAddressSet();

            Assert(input.Value > 0, "Invalid max miners count.");

            RequiredMaximumMinersCountControllerSet();
            Assert(Context.Sender == State.MaximumMinersCountController.Value.OwnerAddress,
                "No permission to set max miners count.");

            TryToGetCurrentRoundInformation(out Round round);

            State.MaximumMinersCount.Value = input.Value;
            State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
            {
                MinersCount = GetMinersCount(round)
            });

            return new Empty();
        }

        private void RequiredMaximumMinersCountControllerSet()
        {
            if (State.MaximumMinersCountController.Value != null) return;
            EnsureParliamentContractAddressSet();

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
            return new Int32Value
            {
                Value = Math.Min(GetAutoIncreasedMinersCount(), State.MaximumMinersCount.Value)
            };
        }

        private int GetAutoIncreasedMinersCount()
        {
            if (State.BlockchainStartTimestamp.Value == null)
            {
                return AEDPoSContractConstants.SupposedMinersCount;
            }

            return AEDPoSContractConstants.SupposedMinersCount.Add(
                (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                .Div(State.MinerIncreaseInterval.Value).Mul(2));
        }
    }
}