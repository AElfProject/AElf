using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.QuadraticFunding
{
    public partial class QuadraticFundingContract
    {
        public override ProjectList GetAllProjects(Int64Value input)
        {
            return State.ProjectListMap[input.Value];
        }

        public override RankingList GetRankingList(Int64Value input)
        {
            var round = input.Value;
            var rankingList = new RankingList();
            if (State.TotalSupportAreaMap[round] != 0)
            {
                rankingList.Unit = State.SupportPoolMap[round].Div(State.TotalSupportAreaMap[round]);
            }

            rankingList.Projects.AddRange(State.ProjectListMap[round].Value);
            foreach (var projectId in rankingList.Projects)
            {
                var project = State.ProjectMap[projectId];
                if (project == null) continue;
                rankingList.Votes.Add(project.TotalVotes);
                rankingList.Support.Add(project.SupportArea);
                rankingList.Grants.Add(project.Grants);
            }

            return rankingList;
        }

        public override RoundInfo GetRoundInfo(Int64Value input)
        {
            var round = input.Value;
            var roundInfo = new RoundInfo
            {
                StartFrom = State.StartTimeMap[round],
                EndAt = State.EndTimeMap[round],
                Support = State.SupportPoolMap[round],
                PreTaxSupport = State.PreTaxSupportPoolMap[round]
            };
            return roundInfo;
        }

        public override VotingCost GetVotingCost(GetVotingCostInput input)
        {
            var votingCost = new VotingCost();
            var project = State.ProjectMap[input.ProjectId];
            var currentRound = State.CurrentRound.Value;
            votingCost.Votable = project.Round == currentRound &&
                                 Context.CurrentBlockTime < State.EndTimeMap[currentRound];

            var voted = State.VotedMap[input.ProjectId][input.From];
            var votingPoints = input.Votes.Mul(input.Votes.Add(1)).Div(2);
            votingPoints = votingPoints.Add(input.Votes.Mul(voted));
            votingCost.Cost = votingPoints.Mul(State.VotingUnitMap[project.Round]);
            return votingCost;
        }

        public override Grands GetGrandsOf(Int64Value input)
        {
            var grands = new Grands();
            var project = State.ProjectMap[input.Value];
            var round = project.Round;
            if (round == 0)
            {
                return grands;
            }

            grands.Total = project.Grants;
            if (round < State.CurrentRound.Value)
            {
                // Round ends.
                if (State.TotalSupportAreaMap[round] != 0)
                {
                    grands.Total = grands.Total.Add(project.SupportArea.Mul(State.SupportPoolMap[round])
                        .Div(State.TotalSupportAreaMap[round]));
                }
            }

            //Assert(grands.Total > project.Withdrew, "");
            if (grands.Total > project.Withdrew)
            {
                return new Grands();
            }

            grands.Rest = grands.Total.Sub(project.Withdrew);
            return grands;
        }

        public override Project GetProjectOf(Int64Value input)
        {
            return State.ProjectMap[input.Value];
        }
    }
}