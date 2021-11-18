using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.QuadraticFunding
{
    public partial class QuadraticFundingContract
    {
        public override Empty Donate(Int64Value input)
        {
            var fee = input.Value.Mul(State.TaxPoint.Value).Div(10000);
            var support = input.Value.Sub(fee);
            State.Tax.Value = State.Tax.Value.Add(fee);
            var currentRound = State.CurrentRound.Value;
            State.SupportPoolMap[currentRound] = State.SupportPoolMap[currentRound].Add(support);
            State.PreTaxSupportPoolMap[currentRound] = State.PreTaxSupportPoolMap[currentRound].Add(input.Value);
            return new Empty();
        }

        public override Empty UploadProject(Int64Value input)
        {
            var currentRound = State.CurrentRound.Value;
            Assert(Context.CurrentBlockTime < State.EndTimeMap[currentRound], $"Round {currentRound} already ended.");
            var projectId = HashHelper.ComputeFrom(Context.Sender).ToInt64();
            Assert(projectId == input.Value, "No permission.");
            var project = State.ProjectMap[projectId] ?? new Project();
            Assert(project.CreateAt == null, "Project already created.");
            project.Round = currentRound;
            project.CreateAt = Context.CurrentBlockTime;
            State.ProjectMap[projectId] = project;
            var currentRoundProjectList = State.ProjectListMap[currentRound] ?? new ProjectList();
            currentRoundProjectList.Value.Add(projectId);
            State.ProjectListMap[currentRound] = currentRoundProjectList;
            return new Empty();
        }

        public override Empty Vote(VoteInput input)
        {
            var currentRound = State.CurrentRound.Value;
            Assert(Context.CurrentBlockTime < State.EndTimeMap[currentRound]);
            var project = State.ProjectMap[input.ProjectId];
            Assert(project.Round == currentRound,
                $"Project with id {input.ProjectId} isn't in current round {currentRound}");

            var voted = State.VotedMap[input.ProjectId][Context.Sender];
            var votingPoints = input.Votes.Mul(input.Votes.Add(1)).Div(2);
            votingPoints = votingPoints.Add(input.Votes.Mul(voted));
            var cost = votingPoints.Mul(State.VotingUnitMap[currentRound]);

            CheckBalanceAndAllowanceIsGreaterThanOrEqualTo(cost);

            var fee = cost.Mul(State.TaxPoint.Value).Div(10000);
            var grants = cost.Sub(fee);
            State.Tax.Value = State.Tax.Value.Add(fee);

            State.VotedMap[input.ProjectId][Context.Sender] =
                State.VotedMap[input.ProjectId][Context.Sender].Add(input.Votes);
            project.Grants = project.Grants.Add(grants);

            var supportArea = input.Votes.Mul(project.TotalVotes.Sub(voted));
            project.TotalVotes = project.TotalVotes.Add(input.Votes);
            project.SupportArea = supportArea.Add(project.SupportArea);
            State.TotalSupportAreaMap[currentRound] = supportArea.Add(State.TotalSupportAreaMap[currentRound]);

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = cost,
                Symbol = State.VoteSymbol.Value
            });

            return new Empty();
        }

        public override Empty TakeOutGrants(TakeOutGrantsInput input)
        {
            var projectId = HashHelper.ComputeFrom(Context.Sender).ToInt64();
            Assert(projectId == input.ProjectId, "No permission.");
            var project = State.ProjectMap[projectId];
            var grants = GetGrandsOf(new Int64Value {Value = projectId});
            Assert(grants.Rest >= input.Amount, "");
            project.Withdrew = project.Withdrew.Add(input.Amount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Amount = input.Amount,
                Symbol = State.VoteSymbol.Value
            });
            return new Empty();
        }

        private void CheckBalanceAndAllowanceIsGreaterThanOrEqualTo(long cost)
        {
            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = Context.Sender,
                Symbol = State.VoteSymbol.Value
            }).Balance;
            Assert(balance >= cost, $"Insufficient balance of {State.VoteSymbol.Value}: {balance}. {cost} is needed.");
            var allowance = State.TokenContract.GetAllowance.Call(new GetAllowanceInput
            {
                Owner = Context.Sender,
                Symbol = State.VoteSymbol.Value
            }).Allowance;
            Assert(allowance >= cost,
                $"Insufficient allowance of {State.VoteSymbol.Value}: {allowance}. {cost} is needed.");
        }
    }
}