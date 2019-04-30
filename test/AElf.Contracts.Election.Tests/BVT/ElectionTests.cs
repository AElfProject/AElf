using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Vote;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        public ElectionContractTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ElectionContract_RegisterElectionVotingEvent()
        {
            // `RegisterElectionVotingEvent` will be called during AElf Consensus Contract initialization,
            // so we can check corresponding voting item directly.

            var electionVotingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Sponsor = ElectionContractAddress,
                Topic = ElectionContractConstants.Topic
            });

            electionVotingEvent.Topic.ShouldBe(ElectionContractConstants.Topic);
            electionVotingEvent.Options.Count.ShouldBe(0);
            electionVotingEvent.Sponsor.ShouldBe(ElectionContractAddress);
            electionVotingEvent.TotalEpoch.ShouldBe(long.MaxValue);
            electionVotingEvent.CurrentEpoch.ShouldBe(1);
            electionVotingEvent.Delegated.ShouldBe(true);
            electionVotingEvent.ActiveDays.ShouldBe(long.MaxValue);
            electionVotingEvent.AcceptedCurrency.ShouldBe(ElectionContractTestConstants.NativeTokenSymbol);
        }

        /// <summary>
        /// Take first 7 full node key pairs to announce election.
        /// </summary>
        /// <returns>Return 7 candidates key pairs.</returns>
        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_AnnounceElection()
        {
            const int candidatesCount = 7;
            var candidatesKeyPairs = FullNodesKeyPairs.Take(candidatesCount).ToList();

            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);
            balanceBeforeAnnouncing.ShouldBeGreaterThan(ElectionContractConstants.LockTokenForElection);

            candidatesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);

            // Check balance after announcing election.
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);

            // Check changes introduced to Main Chain Miner Election voting item.
            var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConstants.Topic,
                Sponsor = ElectionContractAddress
            });
            votingEvent.Options.Count.ShouldBe(candidatesCount);
            foreach (var candidateKeyPair in candidatesKeyPairs)
            {
                votingEvent.Options.ShouldContain(candidateKeyPair.PublicKey.ToHex());
            }

            return candidatesKeyPairs;
        }

        [Fact]
        public async Task ElectionContract_QuiteElection()
        {
            const int quitCount = 2;

            var candidates = await ElectionContract_AnnounceElection();

            // Check VotingEvent before quiting election.
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Topic = ElectionContractConstants.Topic,
                    Sponsor = ElectionContractAddress
                });
                votingEvent.Options.Count.ShouldBe(candidates.Count);
            }

            var quitCandidates = FullNodesKeyPairs.Take(quitCount).ToList();

            var balancesBeforeQuiting = new Dictionary<ECKeyPair, long>();
            // Record balances before quiting election.
            foreach (var quitCandidate in quitCandidates)
            {
                balancesBeforeQuiting.Add(quitCandidate, await GetNativeTokenBalance(quitCandidate.PublicKey));
            }

            quitCandidates.ForEach(async kp => await QuitElectionAsync(kp));

            // Check balances after quiting election.
            foreach (var quitCandidate in quitCandidates)
            {
                var balance = await GetNativeTokenBalance(quitCandidate.PublicKey);
                balance.ShouldBe(balancesBeforeQuiting[quitCandidate] + ElectionContractConstants.LockTokenForElection);
            }

            // Check VotingEvent after quiting election.
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Topic = ElectionContractConstants.Topic,
                    Sponsor = ElectionContractAddress
                });
                votingEvent.Options.Count.ShouldBe(candidates.Count - quitCount);
            }
        }

        /// <summary>
        /// First 5 candidates will get 1000 votes, next 2 candidates will get 500 votes.
        /// Votes are got from 2 different voters.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ElectionContract_Vote()
        {
            const int votersCount = 2;
            const long amount = 500;
            const int lockTime = 100;

            var candidatesKeyPairs = await ElectionContract_AnnounceElection();
            var candidateKeyPair = candidatesKeyPairs[0];

            var votersKeyPairs = VotersKeyPairs.Take(votersCount).ToList();
            var voterKeyPair = votersKeyPairs[0];
            var balanceBeforeVoting = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            balanceBeforeVoting.ShouldBeGreaterThan(0);

            await VoteToCandidates(votersKeyPairs.Take(InitialMinersCount).ToList(),
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, amount);
            await VoteToCandidates(
                votersKeyPairs.Skip(InitialMinersCount).Take(candidatesKeyPairs.Count - InitialMinersCount).ToList(),
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, amount / 2);

            var actualVotedAmount =
                amount * InitialMinersCount + amount * (candidatesKeyPairs.Count - InitialMinersCount);

            // Check ELF token balance.
            {
                var balance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(balanceBeforeVoting - actualVotedAmount);
            }

            // Check VOTE token balance.
            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(actualVotedAmount);
            }

            // Check voter's Votes information.
            {
                var voterVotes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                {
                    Value = voterKeyPair.PublicKey.ToHex()
                });
                voterVotes.PublicKey.ShouldBe(ByteString.CopyFrom(voterKeyPair.PublicKey));
                voterVotes.ActiveVotesIds.Count.ShouldBe(candidatesKeyPairs.Count);
                voterVotes.AllVotedVotesAmount.ShouldBe(actualVotedAmount);
                voterVotes.ValidVotedVotesAmount.ShouldBe(actualVotedAmount);

                var voterVotesWithRecords = await ElectionContractStub.GetVotesInformationWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithRecords.ActiveVotesRecords.Count.ShouldBe(candidatesKeyPairs.Count);

                var voterVotesWithAllRecords = await ElectionContractStub.GetVotesInformationWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ActiveVotesRecords.Count.ShouldBe(candidatesKeyPairs.Count);
            }

            // Check candidate's Votes information.
            {
                var candidateVotes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                {
                    Value = candidateKeyPair.PublicKey.ToHex()
                });
                candidateVotes.PublicKey.ShouldBe(ByteString.CopyFrom(candidateKeyPair.PublicKey));
                candidateVotes.ObtainedActiveVotesIds.Count.ShouldBe(votersCount);
                candidateVotes.AllObtainedVotesAmount.ShouldBe(amount * 2);
                candidateVotes.ValidObtainedVotesAmount.ShouldBe(amount * 2);

                var candidateVotesWithRecords = await ElectionContractStub.GetVotesInformationWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                candidateVotesWithRecords.ObtainedActiveVotesRecords.Count.ShouldBe(votersCount);

                var voterVotesWithAllRecords = await ElectionContractStub.GetVotesInformationWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ObtainedActiveVotesRecords.Count.ShouldBe(votersCount);
            }

            // Check voter's profit detail.
            {
                var welfareHash = ProfitItemsIds[ProfitType.CitizenWelfare];
                var details = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = welfareHash,
                    Receiver = Address.FromPublicKey(votersKeyPairs.First().PublicKey)
                });
                details.Details.Count.ShouldBe(candidatesKeyPairs.Count);
            }
        }
        
        [Fact]
        public async Task ElectionContract_Withdraw()
        {
            const int amount = 1000;
            const int lockTime = 120;

            var candidateKeyPair = FullNodesKeyPairs[0];
            await AnnounceElectionAsync(candidateKeyPair);

            var voterKeyPair = VotersKeyPairs[0];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);

            // Vote
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), lockTime, amount);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var voteId =
                (await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotesIds.First();

            await ElectionContractStub.ReleaseTreasuryProfits.CallAsync(new ReleaseTreasuryProfitsInput
            {
                MinedBlocks = 1,
                RoundNumber = 10,
                TermNumber = 1
            });

            await NextTerm(InitialMinersKeyPairs[0]);

            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime().AddDays(lockTime));

            // Withdraw
            {
                var executionResult = await WithdrawVotes(voterKeyPair, voteId);
                executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Profit
            var voter = GetProfitContractTester(voterKeyPair);
            await voter.Profit.SendAsync(new ProfitInput {ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare]});

            // Check ELF token balance
            {
                var balance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(beforeBalance);
            }

            // Check VOTE token balance.
            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(0);
            }
        }
    }
}