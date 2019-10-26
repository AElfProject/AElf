using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests
    {
        [Fact]
        public async Task ElectionContract_InitializeTwice_Test()
        {
            var transactionResult = (await ElectionContractStub.InitialElectionContract.SendAsync(
                new InitialElectionContractInput())).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        #region AnnounceElection

        [Fact]
        public async Task ElectionContract_AnnounceElection_TokenNotEnough_Test()
        {
            var candidateKeyPair = VoterKeyPairs[0];
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(candidateKeyPair.PublicKey),
                Symbol = ElectionContractTestConstants.NativeTokenSymbol
            })).Balance;
            var tokenTester = GetTokenContractTester(candidateKeyPair);
            await tokenTester.Transfer.SendAsync(new TransferInput
            {
                Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                Amount = balance - 100,
                To = Address.FromPublicKey(VoterKeyPairs[1].PublicKey),
                Memo = "transfer token to other"
            });
            
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_Twice_Test()
        {
            var candidateKeyPair = (await ElectionContract_AnnounceElection_Test())[0];
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
        }

        #endregion

        #region QuitElection

        [Fact]
        public async Task ElectionContract_QuitElection_NotCandidate_Test()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var transactionResult = await QuitElectionAsync(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Sender is not a candidate").ShouldBeTrue();
        }

        #endregion

        #region Vote

        [Fact]
        public async Task ElectionContract_Vote_Failed_Test()
        {
            var candidateKeyPair = ValidationDataCenterKeyPairs[0];
            var voterKeyPair = VoterKeyPairs[0];

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
                var voterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(voterKeyPair.PublicKey),
                    Symbol = "ELF"
                })).Balance;
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, voterBalance/100000000 + 10);

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
        public async Task ElectionContract_GetVictories_NoCandidate_Test()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_CandidatesNotEnough_Test()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList()
                .ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_NoValidCandidate_Test()
        {
            await NextRound(BootMinerKeyPair);

            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesNotEnough_Test()
        {
            const int amount = 100;

            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var candidates = (await ElectionContractStub.GetCandidates.CallAsync(new Empty())).Value;
            foreach (var fullNodesKeyPair in ValidationDataCenterKeyPairs)
            {
                candidates.ShouldContain(ByteString.CopyFrom(fullNodesKeyPair.PublicKey));
            }

            var validCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList();
            foreach (var keyPair in validCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, amount);
            }

            foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
            }

            foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringInput
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(0);
            }

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Victories should contain all valid candidates.
            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_GetVictories_NotAllCandidatesGetVotes_Test()
        {
            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var validCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in validCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 100);
            }

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return validCandidates;
        }

        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesEnough_Test()
        {
            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var moreVotesCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in moreVotesCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);
            }

            var lessVotesCandidates = ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount).Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            foreach (var validCandidate in moreVotesCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        #endregion

    }
}