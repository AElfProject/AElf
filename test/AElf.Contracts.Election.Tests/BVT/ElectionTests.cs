using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        public const int CandidatesCount = 19;
        
        [Fact]
        public async Task ElectionContract_RegisterElectionVotingEvent()
        {
            // `RegisterElectionVotingEvent` will be called during AElf Consensus Contract initialization,
            // so we can check corresponding voting item directly.

            var electionVotingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });

            electionVotingItem.VotingItemId.ShouldBe(MinerElectionVotingItemId);
            electionVotingItem.Options.Count.ShouldBe(0);
            electionVotingItem.Sponsor.ShouldBe(ElectionContractAddress);
            electionVotingItem.TotalSnapshotNumber.ShouldBe(long.MaxValue);
            electionVotingItem.CurrentSnapshotNumber.ShouldBe(1);
            electionVotingItem.IsLockToken.ShouldBe(false);
            electionVotingItem.EndTimestamp.ShouldBe(new Timestamp {Seconds = long.MaxValue});
            electionVotingItem.AcceptedCurrency.ShouldBe(ElectionContractTestConstants.NativeTokenSymbol);
        }

        /// <summary>
        /// Take first 7 full node key pairs to announce election.
        /// </summary>
        /// <returns>Return 7 candidates key pairs.</returns>
        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_AnnounceElection()
        {
            var candidatesKeyPairs = FullNodesKeyPairs.Take(CandidatesCount).ToList();

            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);
            balanceBeforeAnnouncing.ShouldBeGreaterThan(ElectionContractConstants.LockTokenForElection);

            candidatesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);

            // Check balance after announcing election.
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);

            // Check changes introduced to Main Chain Miner Election voting item.
            var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });
            votingItem.Options.Count.ShouldBe(CandidatesCount);
            foreach (var candidateKeyPair in candidatesKeyPairs)
            {
                votingItem.Options.ShouldContain(candidateKeyPair.PublicKey.ToHex());
            }

            return candidatesKeyPairs;
        }

        [Fact]
        public async Task ElectionContract_AnnounceElectionAgain()
        {
            await ElectionContract_QuiteElection();
            
            var candidatesKeyPair = FullNodesKeyPairs.First();

            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            balanceBeforeAnnouncing.ShouldBeGreaterThan(ElectionContractConstants.LockTokenForElection);

            await AnnounceElectionAsync(candidatesKeyPair);

            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);

            // Check balance after announcing election.
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);
        }
        
        [Fact]
        public async Task ElectionContract_QuiteElection()
        {
            const int quitCount = 2;

            var candidates = await ElectionContract_AnnounceElection();

            // Check VotingEvent before quiting election.
            {
                var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
                {
                    VotingItemId = MinerElectionVotingItemId
                });
                votingItem.Options.Count.ShouldBe(candidates.Count);
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
                var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
                {
                    VotingItemId = MinerElectionVotingItemId
                });
                votingItem.Options.Count.ShouldBe(candidates.Count - quitCount);
            }
        }

        /// <summary>
        /// First 5 candidates will get 1000 votes, next 2 candidates will get 500 votes.
        /// Votes are got from 2 different voters.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_Vote()
        {
            const int votersCount = 2;
            const long amount = 500;
            const int lockTime = 100 * 60 * 60 * 24;

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
                var voterVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringInput
                {
                    Value = voterKeyPair.PublicKey.ToHex()
                });
                voterVotes.PublicKey.ShouldBe(ByteString.CopyFrom(voterKeyPair.PublicKey));
                voterVotes.AllVotedVotesAmount.ShouldBe(actualVotedAmount);
                voterVotes.ActiveVotedVotesAmount.ShouldBe(actualVotedAmount);
                voterVotes.ActiveVotingRecords.Count.ShouldBe(0);// Not filled.

                var voterVotesWithRecords = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithRecords.ActiveVotingRecords.Count.ShouldBe(candidatesKeyPairs.Count);

                var voterVotesWithAllRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                // TODO: Withdraw votes and test this.
                voterVotesWithAllRecords.WithdrawnVotesRecords.Count.ShouldBe(0);
            }

            // Check candidate's Votes information.
            {
                var candidateVotes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringInput
                {
                    Value = candidateKeyPair.PublicKey.ToHex()
                });
                candidateVotes.PublicKey.ShouldBe(ByteString.CopyFrom(candidateKeyPair.PublicKey));
                candidateVotes.AllObtainedVotedVotesAmount.ShouldBe(amount * 2);
                candidateVotes.ObtainedActiveVotedVotesAmount.ShouldBe(amount * 2);
                candidateVotes.ObtainedWithdrawnVotesRecords.Count.ShouldBe(0);// Not filled.

                var candidateVotesWithRecords = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                candidateVotesWithRecords.ObtainedActiveVotingRecords.Count.ShouldBe(votersCount);

                var voterVotesWithAllRecords = await ElectionContractStub.GetCandidateVoteWithAllRecords.CallAsync(
                    new StringInput
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ObtainedWithdrawnVotesRecords.Count.ShouldBe(0);
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

            return candidatesKeyPairs;
        }
        
        [Fact]
        public async Task ElectionContract_Withdraw()
        {
            const int amount = 1000;
            const int lockTime = 120 * 60 * 60 * 24;

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
                (await ElectionContractStub.GetElectorVote.CallAsync(new StringInput
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotingRecordIds.First();

            await ElectionContractStub.ReleaseTreasuryProfits.CallAsync(new ReleaseTreasuryProfitsInput
            {
                MinedBlocks = 1,
                RoundNumber = 10,
                TermNumber = 1
            });

            await NextTerm(InitialMinersKeyPairs[0]);

            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime().AddSeconds(lockTime + 1));

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

        [Fact]
        public async Task ElectionContract_GetCandidates()
        {
            var announcedFullNodesKeyPairs = await ElectionContract_AnnounceElection();
            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            announcedFullNodesKeyPairs.Count.ShouldBe(candidates.Value.Count);
            foreach (var keyPair in announcedFullNodesKeyPairs)
            {
                candidates.Value.ShouldContain(ByteString.CopyFrom(keyPair.PublicKey));
            }
        }
        
        [Fact]
        public async Task ElectionContract_GetCandidateInformation()
        {
            const int roundCount = 5;

            var minerKeyPair = FullNodesKeyPairs[0];

            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await ProduceBlocks(BootMinerKeyPair, 1, true);

            await ProduceBlocks(minerKeyPair, roundCount, true);

            var information = await ElectionContractStub.GetCandidateInformation.CallAsync(new StringInput
            {
                Value = minerKeyPair.PublicKey.ToHex()
            });

            information.PublicKey.ShouldBe(minerKeyPair.PublicKey.ToHex());
        }

        [Fact]
        public async Task ElectionContract_MarkCandidateAsEvilNode()
        {
            await ElectionContract_AnnounceElection();

            var publicKey = FullNodesKeyPairs.First().PublicKey.ToHex();
            var transactionResult = (await ElectionContractStub.UpdateCandidateInformation.SendAsync(new UpdateCandidateInformationInput
            {
                IsEvilNode = true,
                PublicKey = publicKey,
                RecentlyProducedBlocks = 10,
                RecentlyMissedTimeSlots = 100
            })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //get candidate information
            var candidateInformation = await ElectionContractStub.GetCandidateInformation.CallAsync(new StringInput
            {
                Value = publicKey
            });
            
            candidateInformation.ShouldBe(new CandidateInformation());
        }
    }
}