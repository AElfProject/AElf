using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using Xunit;


namespace AElf.Contracts.Consensus.Tests
{
    public sealed class ConsensusElectionTest : ConsensusContractTestBase
    {
        private readonly MockSetup _mock;
        private readonly SimpleExecutingService _simpleExecutingService;

        private const ulong PinMoney = 100;

        public ConsensusElectionTest()
        {
            _simpleExecutingService = GetRequiredService<SimpleExecutingService>() ;
            _mock = GetRequiredService<MockSetup>();
        }

        [Fact(Skip = "Skip for now.")]
        public void AnnounceElectionTest()
        {
            InitializeTesting(out _, out var candidates, out _, out ContractsShim contracts);

            var balance = contracts.BalanceOf(GetAddress(candidates[0]));
            Assert.True(balance >= GlobalConfig.LockTokenForElection);

            contracts.AnnounceElection(candidates[0]);
            var res = contracts.IsCandidate(candidates[0].PublicKey.ToHex());
            Assert.True(res);

            balance = contracts.BalanceOf(GetAddress(candidates[0]));
            Assert.True(balance == PinMoney);
        }

        [Fact(Skip = "Skip for now.")]
        public void QuitElectionTest()
        {
            InitializeTesting(out _, out var candidates, out _, out ContractsShim contracts);

            contracts.AnnounceElection(candidates[1]);

            contracts.QuitElection(candidates[1]);
            var res = contracts.IsCandidate(candidates[1].PublicKey.ToHex());
            Assert.False(res);
        }

        [Fact(Skip = "Skip for now.")]
        public void ReAnnounceElectionTest()
        {
            InitializeTesting(out _, out var candidates, out _, out ContractsShim contracts);

            contracts.AnnounceElection(candidates[2]);
            Assert.True(contracts.IsCandidate(candidates[2].PublicKey.ToHex()));

            contracts.QuitElection(candidates[2]);
            Assert.False(contracts.IsCandidate(candidates[2].PublicKey.ToHex()));

            contracts.AnnounceElection(candidates[2]);
            Assert.True(contracts.IsCandidate(candidates[2].PublicKey.ToHex()));
        }

        [Fact(Skip = "Skip for now.")]
        public void VotedAccountReAnnounceElectionTest()
        {
            InitializeTesting(out _, out var candidates, out var voters, out ContractsShim contracts);

            contracts.AnnounceElection(candidates[3]);
            Assert.True(contracts.IsCandidate(candidates[3].PublicKey.ToHex()));

            contracts.Vote(voters[0], candidates[3].PublicKey.ToHex(), 10_000, 90);

            var ticketsOfCandidate = contracts.GetTicketsInfo(candidates[3].PublicKey.ToHex());
            Assert.True(ticketsOfCandidate.ObtainedTickets > 0);

            contracts.QuitElection(candidates[3]);
            Assert.False(contracts.IsCandidate(candidates[3].PublicKey.ToHex()));

            contracts.AnnounceElection(candidates[3]);
            Assert.True(contracts.IsCandidate(candidates[3].PublicKey.ToHex()));
        }

        [Fact(Skip = "Skip for now.")]
        public void VoteTest()
        {
            InitializeTesting(out _, out var candidates, out var voters, out ContractsShim contracts);

            // Candidate announce election.
            contracts.AnnounceElection(candidates[4]);
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = contracts.BalanceOf(GetAddress(voters[1]));
            Assert.True(balanceOfVoter >= amount);
            contracts.Vote(voters[1], candidates[4].PublicKey.ToHex(), amount, 90);
            var balanceAfterVoting = contracts.BalanceOf(GetAddress(voters[1]));
            Assert.True(balanceOfVoter == balanceAfterVoting + amount);

            // Check tickets of voter
            var ticketsOfVoter = contracts.GetTicketsInfo(voters[1].PublicKey.ToHex());
            Assert.True(ticketsOfVoter.VotingRecords.Count == 1);
            Assert.True(ticketsOfVoter.ObtainedTickets == 0);
            Assert.True(ticketsOfVoter.VotedTickets == amount);
            var votingRecordOfVoter = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfVoter);
            Assert.False(votingRecordOfVoter.IsExpired(1));
            Assert.True(votingRecordOfVoter.Count == amount);
            Assert.True(votingRecordOfVoter.From == voters[1].PublicKey.ToHex());
            Assert.True(votingRecordOfVoter.To == candidates[4].PublicKey.ToHex());

            // Check tickets of candidate
            var ticketsOfCandidate = contracts.GetTicketsInfo(candidates[4].PublicKey.ToHex());
            Assert.True(ticketsOfCandidate.VotingRecords.Count == 1);
            Assert.True(ticketsOfCandidate.VotedTickets == 0);
            Assert.True(ticketsOfCandidate.ObtainedTickets == amount);
            var votingRecordOfCandidate = ticketsOfVoter.VotingRecords.First();
            Assert.NotNull(votingRecordOfCandidate);
            Assert.False(votingRecordOfCandidate.IsExpired(1));
            Assert.True(votingRecordOfCandidate.Count == amount);
            Assert.True(votingRecordOfCandidate.From == voters[1].PublicKey.ToHex());
            Assert.True(votingRecordOfCandidate.To == candidates[4].PublicKey.ToHex());
            // Check tickets of a passerby.
            try
            {
                contracts.GetTicketsInfo(candidates[4].PublicKey.ToHex());
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TicketsNotFound, contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Skip for now.")]
        public void VoteTest_VoteToSomeoneDidNotAnnounceElection()
        {
            InitializeTesting(out _, out var candidates, out var voters, out ContractsShim contracts);

            const ulong amount = 10_000;

            // Voter vote to a passerby.
            var balanceOfVoter = contracts.BalanceOf(GetAddress(voters[2]));
            Assert.True(balanceOfVoter >= amount);

            try
            {
                contracts.Vote(voters[2], candidates[5].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.TargetNotAnnounceElection,
                    contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Skip for now.")]
        public void VoteTest_CandidateVote()
        {
            InitializeTesting(out _, out var candidates, out var voters, out ContractsShim contracts);

            // Candidates announce election.
            contracts.AnnounceElection(candidates[6]);
            contracts.AnnounceElection(candidates[7]);
            const ulong amount = PinMoney / 2;

            // Voter vote to another candidate.
            var balanceOfVoter = contracts.BalanceOf(GetAddress(candidates[7]));
            Assert.True(balanceOfVoter >= amount);
            try
            {
                contracts.Vote(candidates[7], candidates[6].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, contracts.TransactionContext.Trace.StdErr);
            }

            // Voter vote to himself.
            try
            {
                contracts.Vote(candidates[7], candidates[7].PublicKey.ToHex(), amount, 90);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.CandidateCannotVote, contracts.TransactionContext.Trace.StdErr);
            }
        }

        [Fact(Skip = "Skip for now.")]
        public void VoteTest_VoteWithInvalidLockDays()
        {
            InitializeTesting(out _, out var candidates, out var voters, out ContractsShim contracts);

            // Candidate announce election.
            contracts.AnnounceElection(candidates[8]);
            const ulong amount = 10_000;

            // Voter vote to aforementioned candidate.
            var balanceOfVoter = contracts.BalanceOf(GetAddress(voters[3]));
            Assert.True(balanceOfVoter >= amount);

            try
            {
                contracts.Vote(voters[3], candidates[8].PublicKey.ToHex(), amount, 13);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.LockDayIllegal, contracts.TransactionContext.Trace.StdErr);
            }
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        private void InitializeTesting(out List<ECKeyPair> initialMiners, out List<ECKeyPair> candidates,
            out List<ECKeyPair> voters, out ContractsShim contracts)
        {
            // Initial users.
            initialMiners = new List<ECKeyPair>();
            candidates = new List<ECKeyPair>();
            voters = new List<ECKeyPair>();
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                initialMiners.Add(CryptoHelpers.GenerateKeyPair());
                candidates.Add(CryptoHelpers.GenerateKeyPair());
                voters.Add(CryptoHelpers.GenerateKeyPair());
            }

            contracts = new ContractsShim(_mock, _simpleExecutingService);

            // Initial term.
            var initialTerm =
                new Miners {PublicKeys = {initialMiners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(1);
            contracts.InitialTerm(initialMiners[0], initialTerm);

            // Initial balances.
            for (var i = 0; i < GlobalConfig.BlockProducerNumber; i++)
            {
                contracts.InitialBalance(initialMiners[0], GetAddress(candidates[i]),
                    GlobalConfig.LockTokenForElection + PinMoney);
                contracts.InitialBalance(initialMiners[0], GetAddress(voters[i]), 100_000);
            }
        }
    }
}