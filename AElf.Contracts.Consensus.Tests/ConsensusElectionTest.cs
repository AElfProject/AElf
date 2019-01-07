using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Consensus.Tests
{
    [UseAutofacTestFramework]
    public class ConsensusElectionTest
    {
        private readonly ContractsShim _contracts;

        private readonly List<ECKeyPair> _voters = new List<ECKeyPair>();
        
        private readonly List<ECKeyPair> _candidates = new List<ECKeyPair>();

        private readonly List<ECKeyPair> _initialMiners = new List<ECKeyPair>();

        private const ulong PinMoney = 100;

        public ConsensusElectionTest(MockSetup mock, SimpleExecutingService simpleExecutingService)
        {
            _contracts = new ContractsShim(mock, simpleExecutingService);
        }

        [Fact]
        public void AnnounceElectionTest()
        {
            InitializeToken();

            var balance = _contracts.BalanceOf(GetAddress(_candidates[0]));
            Assert.True(balance >= GlobalConfig.LockTokenForElection);
            
            _contracts.AnnounceElection(_candidates[0]);
            var res = _contracts.IsCandidate(_candidates[0].PublicKey.ToHex());
            Assert.True(res);
            
            balance = _contracts.BalanceOf(GetAddress(_candidates[0]));
            Assert.True(balance == PinMoney);
        }
        
        [Fact]
        public void QuitElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidates[1]);

            _contracts.QuitElection(_candidates[1]);
            var res = _contracts.IsCandidate(_candidates[1].PublicKey.ToHex());
            Assert.False(res);
        }

        [Fact]
        public void ReAnnounceElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidates[2]);
            Assert.True(_contracts.IsCandidate(_candidates[2].PublicKey.ToHex()));

            _contracts.QuitElection(_candidates[2]);
            Assert.False(_contracts.IsCandidate(_candidates[2].PublicKey.ToHex()));

            _contracts.AnnounceElection(_candidates[2]);
            Assert.True(_contracts.IsCandidate(_candidates[2].PublicKey.ToHex()));
        }
        
        [Fact]
        public void VotedAccountReAnnounceElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidates[3]);
            Assert.True(_contracts.IsCandidate(_candidates[3].PublicKey.ToHex()));

            _contracts.Vote(_voters[0], _candidates[3].PublicKey.ToHex(), 10_000, 90);

            var ticketsOfCandidate = _contracts.GetTicketsInfo(_candidates[3].PublicKey.ToHex());
            Assert.True(ticketsOfCandidate.TotalTickets > 0);

            _contracts.QuitElection(_candidates[3]);
            Assert.False(_contracts.IsCandidate(_candidates[3].PublicKey.ToHex()));
            
            _contracts.AnnounceElection(_candidates[3]);
            Assert.True(_contracts.IsCandidate(_candidates[3].PublicKey.ToHex()));
        }

        [Fact]
        public void VoteTest()
        {
            InitializeToken();

            // Candidate announce election.
            _contracts.AnnounceElection(_candidates[4]);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voters[1]));
            Assert.True(balanceOfVoter >= amount);
            _contracts.Vote(_voters[1], _candidates[4].PublicKey.ToHex(), amount, 90);
            var balanceAfterVoting = _contracts.BalanceOf(GetAddress(_voters[1]));
            Assert.True(balanceOfVoter == balanceAfterVoting + amount);
            
            // Check tickets of voter
            var ticketsOfVoter = _contracts.GetTicketsInfo(_voters[1].PublicKey.ToHex());
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfVoter = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfVoter);
            Assert.False(votingRecordOfVoter.IsExpired(1));
            Assert.True(votingRecordOfVoter.Count == amount);
            Assert.True(votingRecordOfVoter.From == _voters[1].PublicKey.ToHex());
            Assert.True(votingRecordOfVoter.To == _candidates[4].PublicKey.ToHex());

            // Check tickets of candidate
            var ticketsOfCandidate = _contracts.GetTicketsInfo(_candidates[4].PublicKey.ToHex());
            Assert.True(ticketsOfCandidate.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfCandidate = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfCandidate);
            Assert.False(votingRecordOfCandidate.IsExpired(1));
            Assert.True(votingRecordOfCandidate.Count == amount);
            Assert.True(votingRecordOfCandidate.From == _voters[1].PublicKey.ToHex());
            Assert.True(votingRecordOfCandidate.To == _candidates[4].PublicKey.ToHex());
            
            // Check tickets of a passerby.
            try
            {
                _contracts.GetTicketsInfo(_candidates[4].PublicKey.ToHex());
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TicketsNotFound, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact]
        public void VoteTest_VoteToSomeoneDidNotAnnounceElection()
        {
            InitializeToken();

            const ulong amount = 10_000;

            // Voter vote to a passerby.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voters[2]));
            Assert.True(balanceOfVoter >= amount);
            
            try
            {
                _contracts.Vote(_voters[2], _candidates[5].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TargetNotAnnounceElection, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact]
        public void VoteTest_CandidateVote()
        {
            InitializeToken();

            // Candidates announce election.
            _contracts.AnnounceElection(_candidates[6]);
            _contracts.AnnounceElection(_candidates[7]);
            
            const ulong amount = PinMoney / 2;

            // Voter vote to another candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_candidates[7]));
            Assert.True(balanceOfVoter >= amount);
            try
            {
                _contracts.Vote(_candidates[7], _candidates[6].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _contracts.TransactionContext.Trace.StdErr);
            }
            
            // Voter vote to himself.
            try
            {
                _contracts.Vote(_candidates[7], _candidates[7].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact]
        public void VoteTest_VoteWithInvalidLockDays()
        {
            InitializeToken();

            // Candidate announce election.
            _contracts.AnnounceElection(_candidates[8]);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voters[3]));
            Assert.True(balanceOfVoter >= amount);

            try
            {
                _contracts.Vote(_voters[3], _candidates[8].PublicKey.ToHex(), amount, 13);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.LockDayIllegal, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }
        
        private void InitializeMiners()
        {
            _initialMiners.Clear();
            
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                _initialMiners.Add(new KeyPairGenerator().Generate());
                _candidates.Add(new KeyPairGenerator().Generate());
                _voters.Add(new KeyPairGenerator().Generate());
            }
        }
        
        private void InitializeTerm(ECKeyPair starterKeyPair)
        {
            var initialTerm =
                new Miners {PublicKeys = {_initialMiners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(1);
            _contracts.InitialTerm(starterKeyPair, initialTerm);
        }
        
        private void InitializeToken()
        {
            InitializeMiners();
            InitializeTerm(_initialMiners[0]);

            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                _contracts.InitialBalance(_initialMiners[0], GetAddress(_candidates[i]), GlobalConfig.LockTokenForElection + PinMoney);
                _contracts.InitialBalance(_initialMiners[0], GetAddress(_voters[i]), 100_000);
            }
        }
    }
}