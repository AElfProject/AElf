using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
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
        public async Task ElectionContract_NextTerm()
        {
            await NextTerm(InitialMinersKeyPairs[0]);
            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.TermNumber.ShouldBe(2);
        }

        [Fact]
        public async Task ElectionContract_NormalBlock()
        {
            await NormalBlock(InitialMinersKeyPairs[0]);
            var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
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

        private async Task<TransactionResult> VoteToCandidate(ECKeyPair voterKeyPair, string candidatePublicKey,
            int lockTime, long amount)
        {
            var electionStub = GetElectionContractTester(voterKeyPair);
            return (await electionStub.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePublicKey = candidatePublicKey,
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.ToUniversalTime().AddDays(lockTime).ToTimestamp()
            })).TransactionResult;
        }
        
        private async Task VoteToCandidate(List<ECKeyPair> votersKeyPairs, string candidatePublicKey,
            int lockTime, long amount)
        {
            foreach (var voterKeyPair in votersKeyPairs)
            {
                await VoteToCandidate(voterKeyPair, candidatePublicKey, lockTime, amount);
            }
        }
        
        private async Task VoteToCandidates(List<ECKeyPair> votersKeyPairs, List<string> candidatesPublicKeys,
            int lockTime, long amount)
        {
            foreach (var candidatePublicKey in candidatesPublicKeys)
            {
                await VoteToCandidate(votersKeyPairs, candidatePublicKey, lockTime, amount);
            }
        }

        private async Task<TransactionResult> WithdrawVotes(ECKeyPair keyPair, Hash voteId)
        {
            var electionStub = GetElectionContractTester(keyPair);
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