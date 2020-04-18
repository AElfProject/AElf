using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var exception = await ElectionContractStub.InitialElectionContract.SendAsync(
                new InitialElectionContractInput()).ShouldThrowAsync<Exception>();

            exception.Message.ShouldContain("Already initialized.");
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
                Amount = balance / 2,
                To = Address.FromPublicKey(VoterKeyPairs[1].PublicKey),
                Memo = "transfer token to other"
            });
            
            var exception = await AnnounceElectionAsync(candidateKeyPair).ShouldThrowAsync<Exception>();
            exception.Message.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_Twice_Test()
        {
            var s = Stopwatch.StartNew();
            s.Start();
            var candidateKeyPair = (await ElectionContract_AnnounceElection_Test())[0];
            var exception = await AnnounceElectionAsync(candidateKeyPair).ShouldThrowAsync<Exception>();
            exception.Message.ShouldContain("This public key already announced election.");
            s.Stop();
            _testOutputHelper.WriteLine(s.ElapsedMilliseconds.ToString());
        }

        #endregion

        #region QuitElection

        [Fact]
        public async Task ElectionContract_QuitElection_NotCandidate_Test()
        {
            var userKeyPair = SampleECKeyPairs.KeyPairs[2];

            var exception = await QuitElectionAsync(userKeyPair).ShouldThrowAsync<Exception>();
            exception.Message.ShouldContain("Sender is not a candidate");
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
                var exception =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, 100)
                        .ShouldThrowAsync<Exception>();

                exception.Message.ShouldContain("Candidate not found");
            }

            await AnnounceElectionAsync(candidateKeyPair);

            // Voter token not enough
            {
                var voterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(voterKeyPair.PublicKey),
                    Symbol = "ELF"
                })).Balance;
                var exception =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400,
                        voterBalance + 10).ShouldThrowAsync<Exception>();

                exception.Message.ShouldContain("Insufficient balance");
            }

            // Lock time is less than 90 days
            {
                var exception =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 80 * 86400, 1000)
                        .ShouldThrowAsync<Exception>();

                exception.Message.ShouldContain("lock time");
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
        public async Task ElectionContract_ToBecomeValidationDataCenter_Test()
        {
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(25))
            {
                await AnnounceElectionAsync(keyPair);
            }
            
            //add new candidate and vote into data center
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);

            var voter = VoterKeyPairs.First();
            await
                VoteToCandidate(voter, newCandidate.PublicKey.ToHex(), 100 * 86400, 200);

            var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
            victories.Value.Select(o=>o.ToHex()).ShouldContain(newCandidate.PublicKey.ToHex());
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
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
            }

            foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
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