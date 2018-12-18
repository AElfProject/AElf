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
        private readonly ConsensusContractShim _consensusContract;
        private readonly MockSetup _mock;

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
            _mock = mock;
            _consensusContract = new ConsensusContractShim(mock);
        }

        private void InitializeToken()
        {
            _consensusContract.Initialize("ELF", "AElf Token", 100_000_000, 2);
            
            _consensusContract.Transfer(GetAddress(_candidate1), GlobalConfig.LockTokenForElection + PinMoney);
            _consensusContract.Transfer(GetAddress(_candidate2), GlobalConfig.LockTokenForElection + PinMoney);
            _consensusContract.Transfer(GetAddress(_candidate3), GlobalConfig.LockTokenForElection + PinMoney);
            _consensusContract.Transfer(GetAddress(_candidate4), GlobalConfig.LockTokenForElection + PinMoney);
            
            _consensusContract.Transfer(GetAddress(_voter1), 100_000);
            _consensusContract.Transfer(GetAddress(_voter2), 100_000);
            _consensusContract.Transfer(GetAddress(_voter3), 100_000);
            _consensusContract.Transfer(GetAddress(_voter4), 100_000);
        }

        [Fact(Skip = "Time consuming")]
        public void AnnounceElectionTest()
        {
            InitializeToken();

            var balance = _consensusContract.BalanceOf(GetAddress(_candidate1));
            Assert.True(balance >= GlobalConfig.LockTokenForElection);
            
            _consensusContract.AnnounceElection(_candidate1);
            var res = _consensusContract.IsCandidate(_candidate1.PublicKey.ToHex());
            Assert.True(res);
            
            balance = _consensusContract.BalanceOf(GetAddress(_candidate1));
            Assert.True(balance == PinMoney);
        }
        
        [Fact(Skip = "Time consuming")]
        public void QuitElectionTest()
        {
            InitializeToken();

            _consensusContract.AnnounceElection(_candidate2);

            _consensusContract.QuitElection(_candidate2);
            var res = _consensusContract.IsCandidate(_candidate2.PublicKey.ToHex());
            Assert.False(res);
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest()
        {
            InitializeToken();

            // Candidate announce election.
            _consensusContract.AnnounceElection(_candidate2);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _consensusContract.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);
            _consensusContract.Vote(_voter1, _candidate2, amount, 90);
            var balanceAfterVoting = _consensusContract.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter == balanceAfterVoting + amount);
            
            // Check tickets of voter
            var ticketsOfVoter = _consensusContract.GetTicketsInfo(_voter1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfVoter = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfVoter);
            Assert.False(votingRecordOfVoter.IsExpired());
            Assert.True(votingRecordOfVoter.Count == amount);
            Assert.True(votingRecordOfVoter.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfVoter.To == _candidate2.PublicKey.ToHex());

            // Check tickets of candidate
            var ticketsOfCandidate = _consensusContract.GetTicketsInfo(_candidate2);
            Assert.True(ticketsOfCandidate.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.TotalTickets == amount);
            var votingRecordOfCandidate = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfCandidate);
            Assert.False(votingRecordOfCandidate.IsExpired());
            Assert.True(votingRecordOfCandidate.Count == amount);
            Assert.True(votingRecordOfCandidate.From == _voter1.PublicKey.ToHex());
            Assert.True(votingRecordOfCandidate.To == _candidate2.PublicKey.ToHex());
            
            // Check tickets of a passerby.
            try
            {
                _consensusContract.GetTicketsInfo(_candidate3);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TicketsNotFound, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_VoteToSomeoneDidNotAnnounceElection()
        {
            InitializeToken();

            const ulong amount = 10_000;

            // Voter vote to a passerby.
            var balanceOfVoter = _consensusContract.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);
            
            try
            {
                _consensusContract.Vote(_voter1, _candidate2, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TargetNotAnnounceElection, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_CandidateVote()
        {
            InitializeToken();

            // Candidates announce election.
            _consensusContract.AnnounceElection(_candidate1);
            _consensusContract.AnnounceElection(_candidate2);
            
            const ulong amount = PinMoney / 2;

            // Voter vote to another candidate.
            var balanceOfVoter = _consensusContract.BalanceOf(GetAddress(_candidate2));
            Assert.True(balanceOfVoter >= amount);
            try
            {
                _consensusContract.Vote(_candidate2, _candidate1, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _consensusContract.TransactionContext.Trace.StdErr);
            }
            
            // Voter vote to himself.
            try
            {
                _consensusContract.Vote(_candidate2, _candidate2, amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Time consuming")]
        public void VoteTest_VoteWithInvalidLockDays()
        {
            InitializeToken();

            // Candidate announce election.
            _consensusContract.AnnounceElection(_candidate2);
            
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = _consensusContract.BalanceOf(GetAddress(_voter1));
            Assert.True(balanceOfVoter >= amount);

            try
            {
                _consensusContract.Vote(_voter1, _candidate2, amount, 13);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.LockDayIllegal, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(_mock.ChainId.DumpByteArray(), keyPair.PublicKey);
        }
    }
}