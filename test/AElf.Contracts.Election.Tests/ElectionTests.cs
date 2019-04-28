using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Utilities;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public class ElectionTests : ElectionContractTestBase
    {
        public ElectionTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ElectionContract_CheckElectionVotingEvent()
        {
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
        public async Task ElectionContract_InitializeMultiTimes()
        {
            var transactionResult = (await ElectionContractStub.InitialElectionContract.SendAsync(
                new InitialElectionContractInput
                {
                    TokenContractSystemName = Hash.Generate(),
                    VoteContractSystemName = Hash.Generate()
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_CheckCandidates()
        {
            const int announceCount = 7;

            var candidatesKeyPairs = FullNodesKeyPairs.Take(announceCount).ToList();
            candidatesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var voteEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = ElectionContractAddress
            });
            voteEvent.Options.Count.ShouldBe(announceCount);
            foreach (var candidateKeyPair in candidatesKeyPairs)
            {
                voteEvent.Options.ShouldContain(candidateKeyPair.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_TokenNotEnough()
        {
            var candidateKeyPair = VotersKeyPairs[0];
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task<ECKeyPair> ElectionContract_AnnounceElection_Success()
        {
            var candidateKeyPair = FullNodesKeyPairs[0];
            var balanceBeforeAnnouncement = await GetNativeTokenBalance(candidateKeyPair.PublicKey);
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = await GetNativeTokenBalance(candidateKeyPair.PublicKey);
            balanceBeforeAnnouncement.ShouldBe(afterBalance + ElectionContractConsts.LockTokenForElection);
            return candidateKeyPair;
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_Twice()
        {
            var candidateKeyPair = await ElectionContract_AnnounceElection_Success();
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
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
        public async Task QuitElection_WithCommonUser()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var transactionResult = await QuitElectionAsync(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Sender is not a candidate").ShouldBeTrue();
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
        public async Task ElectionContract_Vote_Failed()
        {
            var candidateKeyPair = FullNodesKeyPairs[0];
            var voterKeyPair = VotersKeyPairs[0];

            // candidateKeyPair not announced election yet.
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, 100);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Candidate not found");
            }

            await AnnounceElectionAsync(candidateKeyPair);

            // Voter token not enough
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, 100_000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }

            // Lock time is less than 90 days
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 80, 1000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Should lock token for at least 90 days");
            }
        }

        [Fact]
        public async Task ElectionContract_Withdraw()
        {
            const int amount = 1000;

            var candidateKeyPair = FullNodesKeyPairs[0];
            await AnnounceElectionAsync(candidateKeyPair);

            var voterKeyPair = VotersKeyPairs[0];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);

            // Vote
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, amount);
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

            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime().AddDays(121));

            // Withdraw
            {
                await WithdrawVotes(voterKeyPair, voteId);
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
        public async Task ElectionContract_NextTerm()
        {
            await NextTerm(InitialMinersKeyPairs[0]);
            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.TermNumber.ShouldBe(2);
        }

        [Fact]
        public async Task ElectionContract_NormalBlock()
        {
            await NormalBlock(InitialMinersKeyPairs[0]);
            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.GetMinedBlocks().ShouldBe(1);
            round.GetMinedMiners().Count.ShouldBe(1);
        }

        [Fact]
        public async Task ElectionContract_GetVictories_NoCandidate()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var initialMiner in InitialMinersKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }
        
        [Fact]
        public async Task ElectionContract_GetVictories_CandidatesNotEnough()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.Take(InitialMinersCount - 1).ToList()
                .ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var initialMiner in InitialMinersKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_NoValidCandidate()
        {
            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var initialMiner in InitialMinersKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_ValidCandidatesNotEnough()
        {
            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var validCandidates = FullNodesKeyPairs.Take(InitialMinersCount - 1).ToList();
            validCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100, 100));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_NotAllCandidatesGetVotes()
        {
            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var validCandidates = FullNodesKeyPairs.Take(InitialMinersCount).ToList();
            validCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100, 100));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_ValidCandidatesEnough()
        {
            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var moreVotesCandidates = FullNodesKeyPairs.Take(InitialMinersCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100, 100));

            var lessVotesCandidates = FullNodesKeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100, 99));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var validCandidate in moreVotesCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_CheckReleasedProfits()
        {
            await ElectionContract_GetVictories_ValidCandidatesEnough();

            await NextRound(BootMinerKeyPair);

            await ProduceBlocks(BootMinerKeyPair, 10, true);

            // Check Treasury situation.
            var profitItems = new Dictionary<ProfitType, ProfitItem>();
            foreach (var profitId in ProfitItemsIds)
            {
                var profitItem = await ProfitContractStub.GetProfitItem.CallAsync(profitId.Value);
                profitItems.Add(profitId.Key, profitItem);
            }

            profitItems.Values.ShouldAllBe(i => i.CurrentPeriod == 2);

            var minersRewardAmountInTheory =
                ElectionContractConsts.VotesTotalSupply - profitItems[ProfitType.Treasury].TotalAmount;
            var actualMinersRewardAmount = profitItems[ProfitType.BasicMinerReward].TotalAmount +
                                     profitItems[ProfitType.VotesWeightReward].TotalAmount +
                                     profitItems[ProfitType.ReElectionReward].TotalAmount;
            actualMinersRewardAmount.ShouldBe(minersRewardAmountInTheory);

        }

        #region Private methods

        private async Task<TransactionResult> AnnounceElectionAsync(ECKeyPair keyPair)
        {
            var electionStub = GetElectionContractTester(keyPair);
            return (await electionStub.AnnounceElection.SendAsync(new Empty())).TransactionResult;
        }

        private async Task<TransactionResult> QuitElectionAsync(ECKeyPair keyPair)
        {
            var electionStub = GetElectionContractTester(keyPair);
            return (await electionStub.QuitElection.SendAsync(new Empty())).TransactionResult;
        }

        private async Task<TransactionResult> VoteToCandidate(ECKeyPair userKeyPair, string candidatePublicKey,
            int days, long amount)
        {
            var electionStub = GetElectionContractTester(userKeyPair);
            return (await electionStub.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePublicKey = candidatePublicKey,
                Amount = amount,
                LockTimeUnit = LockTimeUnit.Days,
                LockTime = days
            })).TransactionResult;
        }

        private async Task<TransactionResult> WithdrawVotes(ECKeyPair userKeyPair, Hash voteId)
        {
            var electionStub = GetElectionContractTester(userKeyPair);
            return (await electionStub.Withdraw.SendAsync(voteId)).TransactionResult;
        }

        private async Task ProduceBlocks(ECKeyPair keyPair, int roundsCount, bool changeTerm = false)
        {
            for (var i = 0; i < roundsCount; i++)
            {
                await NormalBlock(keyPair);
                if (i != roundsCount - 1) continue;
                if (changeTerm)
                {
                    await NextTerm(keyPair);
                }
                else
                {
                    await NextRound(keyPair);
                }
            }
        }

        #endregion
    }
}