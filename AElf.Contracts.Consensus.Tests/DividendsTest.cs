using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Xunit;


namespace AElf.Contracts.Consensus.Tests
{
    public sealed class DividendsTest : ConsensusContractTestBase
    {
        private const int CandidatesCount = 18;
        private const int VotersCount = 10;
        private readonly ContractsShim _contracts;

        private readonly List<ECKeyPair> _initialMiners = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _candidates = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _voters = new List<ECKeyPair>();

        private int MiningInterval => 1;

        public DividendsTest()
        {
            _contracts = GetRequiredService<ContractsShim>();
        }

        [Fact(Skip = "Time consuming.")]
        public void DividendsInitializationTest()
        {
            InitializeMiners();
            InitializeTerm(_initialMiners[0]);
            Assert.True(_contracts.BalanceOf(_contracts.ConsensusContractAddress) > 0);
            Assert.True(_contracts.BalanceOf(_contracts.DividendsContractAddress) > 0);
        }

        [Fact(Skip = "Time consuming.")]
        public void ReceiveDividendsTest()
        {
            DividendsInitializationTest();

            InitializeCandidates();
            InitializeVoters();

            var history0 = _contracts.GetCandidatesHistoryInfo();

            var candidatesList = _contracts.GetCandidatesListToFriendlyString();
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
            Assert.Contains(_candidates[1].PublicKey.ToHex(), candidatesList);

            var balanceOfConsensusContract = _contracts.BalanceOf(_contracts.ConsensusContractAddress);
            Assert.True(balanceOfConsensusContract > 0);

            ECKeyPair mustVotedVoter = null;
            // Vote to candidates randomized
            foreach (var voter in _voters)
            {
                foreach (var candidate in _candidates)
                {
                    mustVotedVoter = voter;
                    _contracts.Vote(voter, candidate.PublicKey.ToHex(), (ulong) new Random().Next(1, 100), 90);
                }
            }

            Assert.NotNull(mustVotedVoter);

            var ticketsInformationInJson = _contracts.GetTicketsInfoToFriendlyString(mustVotedVoter.PublicKey.ToHex());
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);

            var ticketsInformation = _contracts.GetTicketsInfo(mustVotedVoter.PublicKey.ToHex());
            var pagedTicketsInformation = _contracts.GetPageableTicketsInfo(mustVotedVoter.PublicKey.ToHex(), 0, 100);
            var votedTickets = ticketsInformation.VotedTickets;
            var balanceAfterVoting = _contracts.BalanceOf(GetAddress(mustVotedVoter));
            Assert.True(votedTickets + balanceAfterVoting == 100_000);

            var transactionId = pagedTicketsInformation.VoteToTransactions.FirstOrDefault()?.ToHex();
            Assert.NotNull(transactionId);

            var history1 = _contracts.GetCandidatesHistoryInfo();

            // Get victories of first term of election, they are miners then.
            var victories = _contracts.GetCurrentVictories().Values;

            // Second term.
            var secondTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, _contracts.GetCurrentRoundNumber(),
                _contracts.GetCurrentTermNumber());
            var candidateForTerm2 = _candidates.First(c => c.PublicKey.ToHex() == victories[1]);
            _contracts.NextTerm(candidateForTerm2, secondTerm);
            _contracts.SnapshotForTerm(candidateForTerm2, secondTerm.TermNumber - 1,
                secondTerm.FirstRound.RoundNumber - 1);
            _contracts.SnapshotForMiners(candidateForTerm2, secondTerm.TermNumber - 1,
                secondTerm.FirstRound.RoundNumber - 1);
            _contracts.SendDividends(candidateForTerm2, secondTerm.TermNumber - 1,
                secondTerm.FirstRound.RoundNumber - 1);
            var roundInfoBeforeTerm2 = _contracts.GetRoundInfo(secondTerm.FirstRound.RoundNumber - 1);
            Debug.WriteLine("Term message:");
            Debug.WriteLine(_contracts.TransactionContext.Trace.StdErr);
            Assert.Equal(2.ToString(), _contracts.GetCurrentTermNumber().ToString());
            var dividendsOfTerm1 = _contracts.GetTermDividends(1);
            Assert.True(dividendsOfTerm1 > 0);

            var secondRound = _contracts.GetRoundInfo(2);

            // New miners produce some blocks.
            var inValuesList = new Stack<Hash>();
            var outValuesList = new Stack<Hash>();
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                var inValue = Hash.Generate();
                inValuesList.Push(inValue);
                outValuesList.Push(Hash.FromMessage(inValue));
            }

            foreach (var newMiner in victories)
            {
                _contracts.PackageOutValue(GetCandidateKeyPair(newMiner), new ToPackage
                {
                    OutValue = outValuesList.Pop(),
                    RoundId = secondRound.RoundId,
                    Signature = Hash.Default
                });

                _contracts.BroadcastInValue(GetCandidateKeyPair(newMiner), new ToBroadcast
                {
                    InValue = inValuesList.Pop(),
                    RoundId = secondRound.RoundId
                });
            }

            // Third item.
            var thirdTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, _contracts.GetCurrentRoundNumber(),
                _contracts.GetCurrentTermNumber());
            var candidateForTerm3 = _candidates.First(c => c.PublicKey.ToHex() == victories[1]);
            _contracts.NextTerm(candidateForTerm3, thirdTerm);
            _contracts.SnapshotForTerm(candidateForTerm3, thirdTerm.TermNumber - 1,
                thirdTerm.FirstRound.RoundNumber - 1);
            _contracts.SnapshotForMiners(candidateForTerm3, thirdTerm.TermNumber - 1,
                thirdTerm.FirstRound.RoundNumber - 1);
            _contracts.SendDividends(candidateForTerm3, thirdTerm.TermNumber - 1,
                thirdTerm.FirstRound.RoundNumber - 1);
            Debug.WriteLine("Term message:");
            Debug.WriteLine(_contracts.TransactionContext.Trace.StdErr);
            Assert.Equal(3.ToString(), _contracts.GetCurrentTermNumber().ToString());
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);

            var history3 = _contracts.GetCandidatesHistoryInfo();
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
            Assert.NotNull(history3);

            var snapshotOfSecondTerm = _contracts.GetTermSnapshot(2);
            Assert.Equal(18.ToString(), snapshotOfSecondTerm.TotalBlocks.ToString());

            var dividendsOfSecondTerm = _contracts.GetTermDividends(2);
            var shouldBe = (ulong) (18 * DPoSContractConsts.ElfTokenPerBlock * 0.2);
            Assert.True(dividendsOfSecondTerm == shouldBe);
            var availableDividends = _contracts.GetAllAvailableDividends(mustVotedVoter.PublicKey.ToHex());
            var balanceBefore = _contracts.BalanceOf(GetAddress(mustVotedVoter));
            _contracts.ReceiveAllDividends(mustVotedVoter);
            var balanceAfter = _contracts.BalanceOf(GetAddress(mustVotedVoter));
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);

            _contracts.WithdrawByTransactionId(mustVotedVoter, transactionId);
            var balanceAfterWithdrawByTxId = _contracts.BalanceOf(GetAddress(mustVotedVoter));
            Assert.True(balanceAfterWithdrawByTxId > balanceAfter);

            _contracts.WithdrawByTransactionId(mustVotedVoter, transactionId);

            _contracts.WithdrawAll(mustVotedVoter);
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
            var balanceAfterWithdraw = _contracts.BalanceOf(GetAddress(mustVotedVoter));
            Assert.True(balanceAfterWithdraw > balanceAfter);

            var history4 = _contracts.GetCandidatesHistoryInfo();
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
            Assert.NotNull(history4);

            var dividendsList = _contracts.CheckDividendsOfPreviousTerm();
            Assert.True(dividendsList.Values.Any());
        }

        private ECKeyPair GetCandidateKeyPair(string publicKey)
        {
            return _candidates.First(c => c.PublicKey.ToHex() == publicKey);
        }

        private void InitializeMiners()
        {
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                _initialMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
        }

        private void InitializeCandidates()
        {
            for (var i = 0; i < CandidatesCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                _candidates.Add(keyPair);
                // Enough for him to announce election
                _contracts.InitialBalance(_initialMiners[0], GetAddress(keyPair), GlobalConfig.LockTokenForElection);
                _contracts.AnnounceElection(keyPair);
            }
        }

        private void InitializeVoters()
        {
            for (var i = 0; i < VotersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                _voters.Add(keyPair);
                // Send them some tokens to vote.
                _contracts.InitialBalance(_initialMiners[0], GetAddress(keyPair), 100_000);
            }
        }

        private void InitializeTerm(ECKeyPair starterKeyPair)
        {
            var initialTerm =
                new Miners {PublicKeys = {_initialMiners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(
                    MiningInterval, 0);
            _contracts.InitialTerm(starterKeyPair, initialTerm);
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }
    }
}