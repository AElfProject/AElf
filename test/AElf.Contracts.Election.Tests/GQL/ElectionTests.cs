using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
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
        [Fact]
        public async Task ElectionContract_InitializeTwice()
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

        #region AnnounceElection

        [Fact]
        public async Task ElectionContract_AnnounceElection_TokenNotEnough()
        {
            var candidateKeyPair = VotersKeyPairs[0];
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_Twice()
        {
            var candidateKeyPair = (await ElectionContract_AnnounceElection())[0];
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
        }

        #endregion

        #region QuitElection

        [Fact]
        public async Task ElectionContract_QuitElection_NotCandidate()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var transactionResult = await QuitElectionAsync(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Sender is not a candidate").ShouldBeTrue();
        }

        #endregion

        #region Vote

        [Fact]
        public async Task ElectionContract_Vote_Failed()
        {
            var candidateKeyPair = FullNodesKeyPairs[0];
            var voterKeyPair = VotersKeyPairs[0];

            // candidateKeyPair not announced election yet.
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, 100);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Candidate not found");
            }

            await AnnounceElectionAsync(candidateKeyPair);

            // Voter token not enough
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, 100_000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }

            // Lock time is less than 90 days
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 80 * 86400, 1000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("lock time");
            }
        }

        #endregion

        #region GetVictories

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
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, amount));

            foreach (var votedFullNodeKeyPair in FullNodesKeyPairs.Take(InitialMinersCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
            }

            foreach (var votedFullNodeKeyPair in FullNodesKeyPairs.Skip(InitialMinersCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(0);
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
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 100));

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

            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {Owner = ElectionContractAddress, Symbol = "VOTE"});

            var moreVotesCandidates = FullNodesKeyPairs.Take(InitialMinersCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

            var lessVotesCandidates = FullNodesKeyPairs.Skip(InitialMinersCount).Take(InitialMinersCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VotersKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            victories.Count.ShouldBe(InitialMinersCount);
            foreach (var validCandidate in moreVotesCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        #endregion

    }
}