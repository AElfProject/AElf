using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
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
            var transactionResult = (await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
            {
                TokenContractSystemName = Hash.Generate(),
                VoteContractSystemName = Hash.Generate()
            })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
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
        
        private async Task<TransactionResult> UserVoteForCandidate(ECKeyPair userKeyPair, string candidatePublicKey, int days, long amount)
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