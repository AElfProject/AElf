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
                Topic = ElectionContractConsts.Topic
            });

            electionVotingEvent.Topic.ShouldBe(ElectionContractConsts.Topic);
            electionVotingEvent.Options.Count.ShouldBe(0);
            electionVotingEvent.Sponsor.ShouldBe(ElectionContractAddress);
            electionVotingEvent.TotalEpoch.ShouldBe(long.MaxValue);
            electionVotingEvent.CurrentEpoch.ShouldBe(1);
            electionVotingEvent.Delegated.ShouldBe(true);
            electionVotingEvent.ActiveDays.ShouldBe(long.MaxValue);
            electionVotingEvent.AcceptedCurrency.ShouldBe(ElectionContractTestConsts.NativeTokenSymbol);
        }

        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_AnnounceElection()
        {
            const int candidatesCount = 7;
            var candidatesKeyPairs = FullNodesKeyPairs.Take(candidatesCount).ToList();

            var balanceBeforeAnnouncement = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);

            candidatesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var balanceAfterAnnouncement = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);

            balanceBeforeAnnouncement.ShouldBe(balanceAfterAnnouncement + ElectionContractConsts.LockTokenForElection);

            var voteEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = ElectionContractAddress
            });
            voteEvent.Options.Count.ShouldBe(candidatesCount);
            foreach (var candidateKeyPair in candidatesKeyPairs)
            {
                voteEvent.Options.ShouldContain(candidateKeyPair.PublicKey.ToHex());
            }

            return candidatesKeyPairs;
        }
        
        [Fact]
        public async Task ElectionContract_QuiteElection()
        {
            const int announceCount = 7;
            const int quitCount = 2;
            FullNodesKeyPairs.Take(announceCount).ToList().ForEach(async kp => await AnnounceElectionAsync(kp));

            // Check VotingEvent before quiting election.
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Topic = ElectionContractConsts.Topic,
                    Sponsor = ElectionContractAddress
                });
                votingEvent.Options.Count.ShouldBe(announceCount);
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
                balance.ShouldBe(balancesBeforeQuiting[quitCandidate] + ElectionContractConsts.LockTokenForElection);
            }

            // Check VotingEvent after quiting election.
            {
                var votingEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
                {
                    Topic = ElectionContractConsts.Topic,
                    Sponsor = ElectionContractAddress
                });
                votingEvent.Options.Count.ShouldBe(announceCount - quitCount);
            }
        }
        
        [Fact]
        public async Task ElectionContract_Vote()
        {
            const int amount = 500;

            var candidateKeyPair = FullNodesKeyPairs[0];
            await AnnounceElectionAsync(candidateKeyPair);

            var voterKeyPair = VotersKeyPairs[0];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            beforeBalance.ShouldBeGreaterThan(0);

            var transactionResult =
                await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 100, amount);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check ELF token balance.
            {
                var balance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(beforeBalance - amount);
            }

            // Check VOTE token balance.
            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(amount);
            }

            // Check voter's Votes information.
            {
                var voterVotes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                {
                    Value = voterKeyPair.PublicKey.ToHex()
                });
                voterVotes.PublicKey.ShouldBe(ByteString.CopyFrom(voterKeyPair.PublicKey));
                voterVotes.ActiveVotesIds.Count.ShouldBe(1);
                voterVotes.AllVotedVotesAmount.ShouldBe(amount);
                voterVotes.ValidVotedVotesAmount.ShouldBe(amount);

                var voterVotesWithRecords = await ElectionContractStub.GetVotesInformationWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithRecords.ActiveVotesRecords.Count.ShouldBe(1);

                var voterVotesWithAllRecords = await ElectionContractStub.GetVotesInformationWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ActiveVotesRecords.Count.ShouldBe(1);
            }

            // Check candidate's Votes information.
            {
                var candidateVotes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                {
                    Value = candidateKeyPair.PublicKey.ToHex()
                });
                candidateVotes.PublicKey.ShouldBe(ByteString.CopyFrom(candidateKeyPair.PublicKey));
                candidateVotes.ObtainedActiveVotesIds.Count.ShouldBe(1);
                candidateVotes.AllObtainedVotesAmount.ShouldBe(amount);
                candidateVotes.ValidObtainedVotesAmount.ShouldBe(amount);

                var candidateVotesWithRecords = await ElectionContractStub.GetVotesInformationWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                candidateVotesWithRecords.ObtainedActiveVotesRecords.Count.ShouldBe(1);

                var voterVotesWithAllRecords = await ElectionContractStub.GetVotesInformationWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ObtainedActiveVotesRecords.Count.ShouldBe(1);
            }

            // Check voter's profit detail.
            {
                var welfareHash = ProfitItemsIds[ProfitType.CitizenWelfare];
                var details = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    ProfitId = welfareHash,
                    Receiver = Address.FromPublicKey(voterKeyPair.PublicKey)
                });
                details.Details.Count.ShouldBe(1);
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