using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
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
        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesNotEnough()
        {
            const int amount = 100;

            await NextRound(BootMinerKeyPair);

            FullNodesKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var candidates = (await ElectionContractStub.GetCandidates.CallAsync(new Empty())).Value;
            foreach (var fullNodesKeyPair in FullNodesKeyPairs)
            {
                candidates.ShouldContain(ByteString.CopyFrom(fullNodesKeyPair.PublicKey));
            }

            var validCandidates = FullNodesKeyPairs.Take(InitialMinersCount - 1).ToList();
            validCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100, amount));

            foreach (var votedFullNodeKeyPair in FullNodesKeyPairs.Take(InitialMinersCount - 1))
            {
                var votes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ValidObtainedVotesAmount.ShouldBe(amount);
            }

            foreach (var votedFullNodeKeyPair in FullNodesKeyPairs.Skip(InitialMinersCount - 1))
            {
                var votes = await ElectionContractStub.GetVotesInformation.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ValidObtainedVotesAmount.ShouldBe(0);
            }

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Victories should contain all valid candidates.
            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_GetVictories_NotAllCandidatesGetVotes()
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

            return validCandidates;
        }

        [Fact]
        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesEnough()
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

            return victories;
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasury_CandidatesNotEnough()
        {
            await ElectionContract_GetVictories_CandidatesNotEnough();

            await NextTerm(BootMinerKeyPair);

            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var initialMinersKeyPair in InitialMinersKeyPairs)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(initialMinersKeyPair.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasury_NoValidCandidate()
        {
            await ElectionContract_GetVictories_NoValidCandidate();

            await NextTerm(BootMinerKeyPair);

            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var initialMinersKeyPair in InitialMinersKeyPairs)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(initialMinersKeyPair.PublicKey.ToHex());
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasury_ValidCandidatesNotEnough()
        {
            var firstRound = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            
            var victories = await ElectionContract_GetVictories_ValidCandidatesNotEnough();

            await NextTerm(BootMinerKeyPair);

            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var validCandidateKeyPair in victories)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(validCandidateKeyPair);
            }
        }

        [Fact]
        public async Task ElectionContract_ReleaseTreasury_NotAllCandidatesGetVotes()
        {
            var validCandidates = await ElectionContract_GetVictories_NotAllCandidatesGetVotes();

            await NextTerm(BootMinerKeyPair);

            var round = await AElfConsensusContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var validCandidateKeyPair in validCandidates)
            {
                round.RealTimeMinersInformation.Keys.ShouldContain(validCandidateKeyPair.PublicKey.ToHex());
            }
        }
    }
}