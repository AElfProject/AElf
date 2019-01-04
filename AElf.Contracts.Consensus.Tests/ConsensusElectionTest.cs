using System;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Consensus.Tests
{
    [UseAutofacTestFramework]
    public class ConsensusElectionTest
    {
        private readonly ContractsShim _contracts;

        private readonly ECKeyPair _voter1 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _voter2 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _voter3 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _voter4 = new KeyPairGenerator().Generate();
        
        private readonly ECKeyPair _candidate1 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _candidate2 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _candidate3 = new KeyPairGenerator().Generate();
        private readonly ECKeyPair _candidate4 = new KeyPairGenerator().Generate();

        private const ulong PinMoney = 100;

        public ConsensusElectionTest(MockSetup mock)
        {
            _contracts = new ContractsShim(mock);
        }

        private void InitializeToken()
        {
            _contracts.Initialize("ELF", "AElf Token", 100_000_000, 2);
            
            _contracts.Transfer(GetAddress(_candidate1), GlobalConfig.LockTokenForElection + PinMoney);
            _contracts.Transfer(GetAddress(_candidate2), GlobalConfig.LockTokenForElection + PinMoney);
            _contracts.Transfer(GetAddress(_candidate3), GlobalConfig.LockTokenForElection + PinMoney);
            _contracts.Transfer(GetAddress(_candidate4), GlobalConfig.LockTokenForElection + PinMoney);
            
            _contracts.Transfer(GetAddress(_voter1), 100_000);
            _contracts.Transfer(GetAddress(_voter2), 100_000);
            _contracts.Transfer(GetAddress(_voter3), 100_000);
            _contracts.Transfer(GetAddress(_voter4), 100_000);
        }

        [Fact]
        public void AnnounceElectionTest()
        {
            InitializeToken();

            var balance = _contracts.BalanceOf(GetAddress(_candidate1));
            Assert.True(balance >= GlobalConfig.LockTokenForElection);
            
            _contracts.AnnounceElection(_candidate1);
            var res = _contracts.IsCandidate(_candidate1.PublicKey.ToHex());
            Assert.True(res);
            
            balance = _contracts.BalanceOf(GetAddress(_candidate1));
            Assert.True(balance == PinMoney);
        }
        
        [Fact]
        public void QuitElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidate2);

            _contracts.QuitElection(_candidate2);
            var res = _contracts.IsCandidate(_candidate2.PublicKey.ToHex());
            Assert.False(res);
        }

        [Fact]
        public void ReAnnounceElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidate1);
            Assert.True(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));

            _contracts.QuitElection(_candidate1);
            Assert.False(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));

            _contracts.AnnounceElection(_candidate1);
            Assert.True(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));
        }
        
        [Fact]
        public void VotedAccountReAnnounceElectionTest()
        {
            InitializeToken();

            _contracts.AnnounceElection(_candidate1);
            Assert.True(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));

            _contracts.Vote(_voter1, _candidate1, 10_000, 90);

            var ticketsOfCandidate = _contracts.GetTicketsInfo(_candidate1);
            Assert.True(ticketsOfCandidate.TotalTickets > 0);

            _contracts.QuitElection(_candidate1);
            Assert.False(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));
            
            _contracts.AnnounceElection(_candidate1);
            Assert.True(_contracts.IsCandidate(_candidate1.PublicKey.ToHex()));
        }

        [Fact(Skip = "Time consuming.")]
        public void VoteTest()
        {
            InitializeToken();

            // Candidate announce election.
            _contracts.AnnounceElection(_candidate2);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);
            _contracts.Vote(_voter1, _candidate2, amount, 90);
            var balanceAfterVoting = _contracts.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter == balanceAfterVoting + amount);
            
            // Check tickets of voter
            var ticketsOfVoter = _contracts.GetTicketsInfo(_voter1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfVoter = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfVoter);
            Assert.False(votingRecordOfVoter.IsExpired(1));
            Assert.True(votingRecordOfVoter.Count == amount);
            Assert.True(votingRecordOfVoter.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfVoter.To == _candidate2.PublicKey.ToHex());

            // Check tickets of candidate
            var ticketsOfCandidate = _contracts.GetTicketsInfo(_candidate2);
            Assert.True(ticketsOfCandidate.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfCandidate = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfCandidate);
            Assert.False(votingRecordOfCandidate.IsExpired(1));
            Assert.True(votingRecordOfCandidate.Count == amount);
            Assert.True(votingRecordOfCandidate.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfCandidate.To == _candidate2.PublicKey.ToHex());
            
            // Check tickets of a passerby.
            try
            {
                _contracts.GetTicketsInfo(_candidate3);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TicketsNotFound, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_VoteToSomeoneDidNotAnnounceElection()
        {
            InitializeToken();

            const ulong amount = 10_000;

            // Voter vote to a passerby.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);
            
            try
            {
                _contracts.Vote(_voter1, _candidate2, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TargetNotAnnounceElection, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_CandidateVote()
        {
            InitializeToken();

            // Candidates announce election.
            _contracts.AnnounceElection(_candidate1);
            _contracts.AnnounceElection(_candidate2);
            
            const ulong amount = PinMoney / 2;

            // Voter vote to another candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_candidate2));
            Assert.True(balanceOfVoter >= amount);
            try
            {
                _contracts.Vote(_candidate2, _candidate1, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _contracts.TransactionContext.Trace.StdErr);
            }
            
            // Voter vote to himself.
            try
            {
                _contracts.Vote(_candidate2, _candidate2, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_VoteWithInvalidLockDays()
        {
            InitializeToken();

            // Candidate announce election.
            _contracts.AnnounceElection(_candidate2);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _contracts.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);

            try
            {
                _contracts.Vote(_voter1, _candidate2, amount, 13);
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
    }
}