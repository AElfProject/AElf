using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Consensus.Tests
{
    [UseAutofacTestFramework]
    public class TermTest
    {
        private const int CandidatesCount = 20;
        private const int VotersCount = 10;
        
        private readonly ConsensusContractShim _consensusContract;

        private readonly MockSetup _mock;

        private readonly List<ECKeyPair> _initialMiners = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _candidates = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _voters = new List<ECKeyPair>();

        private int MiningInterval => 1;

        public TermTest(MockSetup mock)
        {
            _mock = mock;
            _consensusContract = new ConsensusContractShim(mock);

            const ulong totalSupply = 100_000_000_000;
            _consensusContract.Initialize("ELF", "AElf Token", totalSupply, 2);

            _consensusContract.Transfer(_consensusContract.DividendsContractAddress, (ulong) (totalSupply * 0.12 * 0.2));
        }
        
        private void InitialMiners()
        {
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                _initialMiners.Add(new KeyPairGenerator().Generate());
            }
        }

        private void InitialCandidates()
        {
            for (var i = 0; i < CandidatesCount; i++)
            {
                var keyPair = new KeyPairGenerator().Generate();
                _candidates.Add(keyPair);
                // Enough for him to announce election
                _consensusContract.Transfer(GetAddress(keyPair), GlobalConfig.LockTokenForElection);
                _consensusContract.AnnounceElection(keyPair);
            }
        }

        private void InitialVoters()
        {
            for (var i = 0; i < VotersCount; i++)
            {
                var keyPair = new KeyPairGenerator().Generate();
                _voters.Add(keyPair);
                // Send them some tokens to vote.
                _consensusContract.Transfer(GetAddress(keyPair), 100_000);
            }
        }

        [Fact]
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
                    if (new Random().Next(0, 100) < 2)
                    {
                        _consensusContract.Vote(voter, candidate, (ulong) new Random().Next(1, 100), 90);
                    }
                }
            }

            // Get victories of first term of election, they are miners then.
            var victories = _consensusContract.GetCurrentVictories().Split(';');
            
            // Next term.
            var nextTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, 2);
            _consensusContract.NextTerm(_candidates.First(c => c.PublicKey.ToHex() == victories[1]), nextTerm);
            Assert.Equal(string.Empty, _consensusContract.TransactionContext.Trace.StdErr);
            
            // Check the information of the last round of previous term.
            // All the initial miners have missed 1 time slot.
            var firstRound = _consensusContract.GetRoundInfo(1);
            foreach (var initialMiner in _initialMiners)
            {
                Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[initialMiner.PublicKey.ToHex()].MissedTimeSlots);
            }
            // And the one started the blockchain has produced one block.
            Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[_initialMiners[0].PublicKey.ToHex()].MissedTimeSlots);

            // Check the mines of new term.
            var firstRoundOfNewTerm = _consensusContract.GetRoundInfo(2);
            Assert.True(!firstRoundOfNewTerm.RealTimeMinersInfo.Keys.Except(victories).Any());
            var secondRoundOfNewTerm = _consensusContract.GetRoundInfo(3);
            Assert.True(!secondRoundOfNewTerm.RealTimeMinersInfo.Keys.Except(victories).Any());
        }
        
        private void InitialTerm(ECKeyPair starterKeyPair)
        {
            var initialTerm =
                new Miners {PublicKeys = {_initialMiners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(MiningInterval);
            _consensusContract.InitialTerm(starterKeyPair, initialTerm);
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(_mock.ChainId.DumpByteArray(), keyPair.PublicKey);
        }
    }
}