using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
        public async Task AnnounceElection_Without_EnoughToken()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[11];

            var transactionResult = await AnnounceElection(userKeyPair);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task AnnounceElection_Success()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[1];
            var beforeBalance = await GetNativeTokenBalance(userKeyPair.PublicKey);

            var transactionResult = await AnnounceElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetNativeTokenBalance(userKeyPair.PublicKey);

            beforeBalance.ShouldBe(afterBalance + ElectionContractConsts.LockTokenForElection);
        }

        [Fact]
        public async Task AnnounceElection_Twice()
        {
            await AnnounceElection_Success();

            var userKeyPair = SampleECKeyPairs.KeyPairs[1];
            var transactionResult = await AnnounceElection(userKeyPair);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
        }

        [Fact]
        public async Task QuitElection_WithCurrentMiner()
        {
            await AnnounceElection_Success();

            var userKeyPair = SampleECKeyPairs.KeyPairs[1];

            var transactionResult = await QuiteElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Current miners cannot quit election").ShouldBeTrue();
        }

        [Fact]
        public async Task QuiteElection_WithCandidate()
        {
            for (var i = 1; i < 5; i++)
            {
                var user = SampleECKeyPairs.KeyPairs[i];
                await AnnounceElection(user);
            }

            var voteEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = ElectionContractAddress
            });
            voteEvent.Options.Count.ShouldBe(4);

            var userKeyPair = SampleECKeyPairs.KeyPairs[4];

            var beforeBalance = await GetNativeTokenBalance(userKeyPair.PublicKey);

            var transactionResult = await QuiteElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetNativeTokenBalance(userKeyPair.PublicKey);
            afterBalance.ShouldBe(beforeBalance + ElectionContractConsts.LockTokenForElection);

            voteEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = ElectionContractAddress
            });
            voteEvent.Options.Count.ShouldBe(3);
        }

        [Fact]
        public async Task QuitElection_WithCommonUser()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var transactionResult = await QuiteElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Sender is not a candidate").ShouldBeTrue();
        }

        [Fact]
        public async Task ElectionContract_Vote()
        {
            const int amount = 500;

            var candidateKeyPair = SampleECKeyPairs.KeyPairs[1];
            await AnnounceElection(candidateKeyPair);

            var voterKeyPair = SampleECKeyPairs.KeyPairs[11];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            beforeBalance.ShouldBeGreaterThan(0);

            var transactionResult = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 100, amount);
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
            }
        }

        [Fact]
        public async Task ElectionContract_Vote_Failed()
        {
            var candidateKeyPair = SampleECKeyPairs.KeyPairs[1];
            var voterKeyPair = SampleECKeyPairs.KeyPairs[11];

            // candidateKeyPair not announced election yet.
            {
                var transactionResult = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, 100);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Candidate state incorrect.").ShouldBeTrue();
            }

            await AnnounceElection(candidateKeyPair);

            // Voter token not enough
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, 100_000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
            }

            // Lock time is less than 90 days
            {
                var transactionResult = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 80, 1000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Should lock token for at least 90 days").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task ElectionContract_Withdraw()
        {
            const int amount = 1000;

            var candidateKeyPair = SampleECKeyPairs.KeyPairs[1];
            await AnnounceElection(candidateKeyPair);

            var voterKeyPair = SampleECKeyPairs.KeyPairs[11];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);

            // Vote
            {
                var transactionResult = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120, amount);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            var voteId =
                (await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotesIds.First();

            BlockTimeProvider.SetBlockTime(StartTimestamp.ToDateTime().AddDays(121));

            // Withdraw
            {
                var transactionResult = await WithdrawVotes(voterKeyPair, voteId);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Check ELF token balance
            {
                var balance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(beforeBalance);
            }

            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task Get_Candidates()
        {
            for (int i = 1; i < 5; i++)
            {
                var candidate = SampleECKeyPairs.KeyPairs[i];
                await AnnounceElection(candidate);
            }

            var voteEvent = await VoteContractStub.GetVotingEvent.CallAsync(new GetVotingEventInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = ElectionContractAddress
            });
            voteEvent.Options.Count.ShouldBe(4);
            voteEvent.Options.Contains(SampleECKeyPairs.KeyPairs[1].PublicKey.ToHex()).ShouldBeTrue();
            voteEvent.Options.Contains(SampleECKeyPairs.KeyPairs[2].PublicKey.ToHex()).ShouldBeTrue();
            voteEvent.Options.Contains(SampleECKeyPairs.KeyPairs[3].PublicKey.ToHex()).ShouldBeTrue();
            voteEvent.Options.Contains(SampleECKeyPairs.KeyPairs[4].PublicKey.ToHex()).ShouldBeTrue();
        }

        #region Private methods

        private async Task<TransactionResult> AnnounceElection(ECKeyPair keyPair)
        {
            var electionStub = GetElectionContractTester(keyPair);
            return (await electionStub.AnnounceElection.SendAsync(new Empty())).TransactionResult;
        }

        private async Task<TransactionResult> QuiteElection(ECKeyPair keyPair)
        {
            var electionStub = GetElectionContractTester(keyPair);
            return (await electionStub.QuitElection.SendAsync(new Empty())).TransactionResult;
        }

        private async Task<TransactionResult> VoteToCandidate(ECKeyPair userKeyPair, string candidatePublicKey,
            int days, long amount)
        {
            var electionStub = GetElectionContractTester(userKeyPair);
            var transactionResult = (await electionStub.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePublicKey = candidatePublicKey,
                Amount = amount,
                LockTimeUnit = LockTimeUnit.Days,
                LockTime = days
            })).TransactionResult;

            return transactionResult;
        }
        
        private async Task<TransactionResult> WithdrawVotes(ECKeyPair userKeyPair, Hash voteId)
        {
            var electionStub = GetElectionContractTester(userKeyPair);
            return (await electionStub.Withdraw.SendAsync(voteId)).TransactionResult;
        }

        #endregion
    }
}