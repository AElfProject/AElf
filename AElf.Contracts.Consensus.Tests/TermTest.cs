using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel;
using Xunit;


namespace AElf.Contracts.Consensus.Tests
{
    public class TermTest
    {
        private const int CandidatesCount = 18;
        private const int VotersCount = 100;
        private readonly ContractsShim _contracts;

        private readonly List<ECKeyPair> _initialMiners = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _candidates = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _voters = new List<ECKeyPair>();

        private int MiningInterval => 1;

        public TermTest(MockSetup mock, SimpleExecutingService simpleExecutingService)
        {
            _contracts = new ContractsShim(mock, simpleExecutingService);
        }

        private void InitialMiners()
        {
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                _initialMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
        }

        private void InitialCandidates()
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

        private void InitialVoters()
        {
            for (var i = 0; i < VotersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                _voters.Add(keyPair);
                // Send them some tokens to vote.
                _contracts.InitialBalance(_initialMiners[0], GetAddress(keyPair), 100_000);
            }
        }

        [Fact(Skip = "Skip for now.")]
        public void ChangeTermTest()
        {
            // At very first, because none account of this blockchain has elf token,
            // we have no choice but appoint some miners to do the initialization.
            InitialMiners();
            InitialTerm(_initialMiners[0]);

            InitialCandidates();
            InitialVoters();

            // Vote to candidates randomized
            foreach (var voter in _voters)
            {
                foreach (var candidate in _candidates)
                {
                    _contracts.Vote(voter, candidate.PublicKey.ToHex(), (ulong) new Random().Next(1, 100), 90);
                }
            }

            // Get victories of first term of election, they are miners then.
            var victories = _contracts.GetCurrentVictories().Values;
            // Next term.
            var nextTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, 2, 2);
            _contracts.NextTerm(_candidates.First(c => c.PublicKey.ToHex() == victories[1]), nextTerm);
            Assert.Equal(string.Empty, _contracts.TransactionContext.Trace.StdErr);
            // Check the information of the last round of previous term.
            // All the initial miners have missed 1 time slot.
            var firstRound = _contracts.GetRoundInfo(1);
            foreach (var initialMiner in _initialMiners)
            {
                Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[initialMiner.PublicKey.ToHex()].MissedTimeSlots);
            }

            // And the one started the blockchain has produced one block.
            Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[_initialMiners[0].PublicKey.ToHex()].MissedTimeSlots);

            // Check the mines of new term.
            var firstRoundOfNewTerm = _contracts.GetRoundInfo(2);
            Assert.True(!firstRoundOfNewTerm.RealTimeMinersInfo.Keys.Except(victories).Any());
            var secondRoundOfNewTerm = _contracts.GetRoundInfo(3);
            Assert.True(!secondRoundOfNewTerm.RealTimeMinersInfo.Keys.Except(victories).Any());
        }

        private void InitialTerm(ECKeyPair starterKeyPair)
        {
            var initialTerm =
                new Miners {PublicKeys = {_initialMiners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(
                    MiningInterval);
            _contracts.InitialTerm(starterKeyPair, initialTerm);
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }
    }
}