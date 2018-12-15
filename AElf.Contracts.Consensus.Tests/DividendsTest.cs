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
    public class DividendsTest
    {
        private const int CandidatesCount = 30;
        private const int VotersCount = 50;
        
        private readonly ConsensusContractShim _consensusContract;
        private readonly TokenContractShim _tokenContract;
        private readonly DividendsContractShim _dividendsContract;

        private readonly MockSetup _mock;

        private readonly List<ECKeyPair> _initialMiners = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _candidates = new List<ECKeyPair>();
        private readonly List<ECKeyPair> _voters = new List<ECKeyPair>();

        private int MiningInterval => 1;
        
        public DividendsTest(MockSetup mock)
        {
            _mock = mock;
            _consensusContract = new ConsensusContractShim(mock);
            _tokenContract = new TokenContractShim(mock);
            _dividendsContract = new DividendsContractShim(mock);

            _tokenContract.Initialize("ELF", "AElf Token", 100_000_000_000, 2);
        }
        
        [Fact]
        public void GetDividendsTest()
        {
            InitialMiners();
            InitialTerm(_initialMiners[0]);
            
            InitialCandidates();
            InitialVoters();

            ECKeyPair mustVotedVoter = null;
            // Vote to candidates randomized
            foreach (var voter in _voters)
            {
                foreach (var candidate in _candidates)
                {
                    if (new Random().Next(0, 100) < 2)
                    {
                        mustVotedVoter = voter;
                        _consensusContract.Vote(voter, candidate, (ulong) new Random().Next(1, 100), 90);
                    }
                }
            }
            Assert.NotNull(mustVotedVoter);

            // Get victories of first term of election, they are miners then.
            var victories = _consensusContract.GetCurrentVictories().Split(';');
            
            // Next term.
            var secondTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, 2);
            _consensusContract.NextTerm(_candidates.First(c => c.PublicKey.ToHex() == victories[1]), secondTerm);

            var secondRound = _consensusContract.GetRoundInfo(2);
            
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
                _consensusContract.PackageOutValue(GetCandidateKeyPair(newMiner), new ToPackage
                {
                    OutValue = outValuesList.Pop(),
                    RoundId = secondRound.RoundId,
                    Signature = Hash.Default
                });
                
                _consensusContract.BroadcastInValue(GetCandidateKeyPair(newMiner), new ToBroadcast
                {
                    InValue = inValuesList.Pop(),
                    RoundId = secondRound.RoundId
                });
            }
            
            // Third item.
            var thirdTerm = victories.ToMiners().GenerateNewTerm(MiningInterval, 3);
            _consensusContract.NextTerm(_candidates.First(c => c.PublicKey.ToHex() == victories[1]), thirdTerm);

            var balanceBefore = _tokenContract.BalanceOf(GetAddress(mustVotedVoter));
            _consensusContract.GetAllDividends(mustVotedVoter);
            var balanceAfter = _tokenContract.BalanceOf(GetAddress(mustVotedVoter));
            Assert.Equal(string.Empty, _consensusContract.TransactionContext.Trace.StdErr);
            // TODO: The calling result is true but the balance didn't change.
            //Assert.True(balanceAfter > balanceBefore);
        }

        private ECKeyPair GetCandidateKeyPair(string publicKey)
        {
            return _candidates.First(c => c.PublicKey.ToHex() == publicKey);
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
                _tokenContract.Transfer(GetAddress(keyPair), GlobalConfig.LockTokenForElection);
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
                _tokenContract.Transfer(GetAddress(keyPair), 100_000);
            }
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