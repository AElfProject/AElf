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
        
        [Fact(Skip = "With issue right now, wait fix.")]
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