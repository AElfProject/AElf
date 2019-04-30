using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
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
                LockTimeUnit = TimeUnit.Days,
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
    }
}