using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Election
{
    public class ElectionTests : ElectionContractTestBase
    {
        public ElectionTests()
        {
            InitializeContracts();
            AsyncHelper.RunSync(async () =>
                await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(
                    new RegisterElectionVotingEventInput()));
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

            var transactionResult = await UserAnnounceElection(userKeyPair);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task AnnounceElection_Success()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[1];
            var beforeBalance = await GetUserBalance(userKeyPair.PublicKey);

            var transactionResult = await UserAnnounceElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetUserBalance(userKeyPair.PublicKey);

            beforeBalance.ShouldBe(afterBalance + ElectionContractConsts.LockTokenForElection);
        }

        [Fact]
        public async Task AnnounceElection_Twice()
        {
            await AnnounceElection_Success();

            var userKeyPair = SampleECKeyPairs.KeyPairs[1];
            var transactionResult = await UserAnnounceElection(userKeyPair);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
        }

        [Fact]
        public async Task QuitElection_WithCandidate()
        {
            await AnnounceElection_Success();

            var userKeyPair = SampleECKeyPairs.KeyPairs[1];
            var beforeBalance = await GetUserBalance(userKeyPair.PublicKey);

            var transactionResult = await UserQuiteElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetUserBalance(userKeyPair.PublicKey);
            afterBalance.ShouldBe(beforeBalance + ElectionContractConsts.LockTokenForElection);
        }

        [Fact]
        public async Task QuitElection_WithCommonUser()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var transactionResult = await UserQuiteElection(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("").ShouldBeTrue();
        }

        [Fact]
        public async Task UserVote_Candidate_Success()
        {
            var candidateUser = SampleECKeyPairs.KeyPairs[1];
            await UserAnnounceElection(candidateUser);

            var voteUser = SampleECKeyPairs.KeyPairs[11];
            var beforeBalance = await GetUserBalance(voteUser.PublicKey);

            var transactionResult = await UserVoteForCandidate(voteUser, candidateUser.PublicKey.ToHex(), 100, 500);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetUserBalance(voteUser.PublicKey);
            afterBalance.ShouldBe(beforeBalance - 500);
        }

        [Fact]
        public async Task UserVote_Candidate_Failed()
        {
            var commonUser = SampleECKeyPairs.KeyPairs[1];
            var voteUser = SampleECKeyPairs.KeyPairs[11];

            //candidate is not in list
            {
                var transactionResult = await UserVoteForCandidate(voteUser, commonUser.PublicKey.ToHex(), 120, 100);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("").ShouldBeTrue();
            }

            await UserAnnounceElection(commonUser);

            //user token is not enough
            {
                var transactionResult =
                    await UserVoteForCandidate(voteUser, commonUser.PublicKey.ToHex(), 120, 100_000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
            }

            //lock time is not over 90 days
            {
                var transactionResult = await UserVoteForCandidate(voteUser, commonUser.PublicKey.ToHex(), 80, 1000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Should lock token for at least 90 days").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UserWithdraw_Success()
        {
            var candidateUser = SampleECKeyPairs.KeyPairs[1];
            await UserAnnounceElection(candidateUser);

            var voteUser = SampleECKeyPairs.KeyPairs[11];
            var beforeBalance = await GetUserBalance(voteUser.PublicKey);

            var transactionResult = await UserVoteForCandidate(voteUser, candidateUser.PublicKey.ToHex(), 120, 1000);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetUserBalance(voteUser.PublicKey);
            afterBalance.ShouldBe(beforeBalance - 1000);
        }

        private async Task<TransactionResult> UserAnnounceElection(ECKeyPair userKeyPair)
        {
            var electionStub = GetElectionContractTester(userKeyPair);

            var transactionResult = (await electionStub.AnnounceElection.SendAsync(new Empty()
            )).TransactionResult;

            return transactionResult;
        }

        private async Task<TransactionResult> UserQuiteElection(ECKeyPair userKeyPair)
        {
            var electionStub = GetElectionContractTester(userKeyPair);

            var transactionResult = (await electionStub.QuitElection.SendAsync(new Empty()
            )).TransactionResult;

            return transactionResult;
        }

        private async Task<TransactionResult> UserVoteForCandidate(ECKeyPair userKeyPair, string candidatePublicKey,
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
    }
}