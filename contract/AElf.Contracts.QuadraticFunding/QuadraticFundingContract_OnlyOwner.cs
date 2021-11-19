using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.QuadraticFunding
{
    public partial class QuadraticFundingContract
    {
        public override Empty RoundOver(Empty input)
        {
            AssertSenderIsOwner();
            var currentRound = State.CurrentRound.Value;
            Assert(Context.CurrentBlockTime > State.EndTimeMap[currentRound] &&
                   State.EndTimeMap[currentRound].Seconds > 0);
            State.CurrentRound.Value = currentRound.Add(1);
            return new Empty();
        }

        public override Empty ChangeOwner(Address input)
        {
            AssertSenderIsOwner();
            State.Owner.Value = input;
            return new Empty();
        }

        public override Empty BanProject(BanProjectInput input)
        {
            AssertSenderIsOwner();
            var project = State.ProjectMap[input.ProjectId];
            var currentRound = State.CurrentRound.Value;
            Assert(project.Round == currentRound, "Incorrect round.");
            if (input.Ban)
            {
                State.BanMap[input.ProjectId] = true;
                State.TotalSupportAreaMap[currentRound] =
                    State.TotalSupportAreaMap[currentRound].Sub(project.SupportArea);
            }
            else
            {
                State.BanMap.Remove(input.ProjectId);
                State.TotalSupportAreaMap[currentRound] =
                    State.TotalSupportAreaMap[currentRound].Add(project.SupportArea);
            }

            Context.Fire(new ProjectBanned
            {
                Project = input.ProjectId,
                Ban = input.Ban
            });
            return new Empty();
        }

        public override Empty SetTaxPoint(Int64Value input)
        {
            AssertSenderIsOwner();
            Assert(input.Value <= MaxTaxPoint, $"Exceeded max tax point: {MaxTaxPoint}");
            State.TaxPoint.Value = input.Value;
            Context.Fire(new TaxPointChanged
            {
                TaxPoint = input.Value
            });
            return new Empty();
        }

        public override Empty SetInterval(Int64Value input)
        {
            AssertSenderIsOwner();
            State.Interval.Value = input.Value;
            Context.Fire(new RoundIntervalChanged
            {
                Interval = input.Value
            });
            return new Empty();
        }

        public override Empty SetVotingUnit(Int64Value input)
        {
            AssertSenderIsOwner();
            State.BasicVotingUnit.Value = input.Value;
            Context.Fire(new VotingUnitChanged
            {
                VotingUnit = input.Value
            });
            return new Empty();
        }

        public override Empty RoundStart(Empty input)
        {
            AssertSenderIsOwner();
            var currentRound = State.CurrentRound.Value;
            Assert(State.EndTimeMap[currentRound] == null, "Round already start.");
            State.VotingUnitMap[currentRound] = State.BasicVotingUnit.Value;
            State.StartTimeMap[currentRound] = Context.CurrentBlockTime;
            State.EndTimeMap[currentRound] = Context.CurrentBlockTime.AddSeconds(State.Interval.Value);
            return new Empty();
        }

        public override Empty Withdraw(Empty input)
        {
            var amount = State.Tax.Value;
            State.Tax.Value = 0;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = State.Owner.Value,
                Amount = amount,
                Symbol = State.VoteSymbol.Value
            });
            return new Empty();
        }

        private void AssertSenderIsOwner()
        {
            Assert(State.Owner.Value != null, "Contract not initialized.");
            Assert(Context.Sender == State.Owner.Value);
        }
    }
}