using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ElectionTest
    {
        public readonly ContractTester<DPoSContractTestAElfModule> Starter;

        private const int MinersCount = 3;

        private const int MiningInterval = 4000;

        public readonly List<ContractTester<DPoSContractTestAElfModule>> Miners;

        public ElectionTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval));
            Miners = Enumerable.Range(0, MinersCount)
                .Select(i => Starter.CreateNewContractTester(minersKeyPairs[i])).ToList();
        }

        [Fact]
        public async Task Announce_Election_Success()
        {
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateInformation = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInformation, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInformation);
            Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);

            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInformation);
            await candidate.AnnounceElectionAsync("AElfin");
            var candidatesList = await candidate.GetCandidatesListAsync();

            // Check the candidates list.
            Assert.Contains(candidate.KeyPair.PublicKey.ToHex(), candidatesList.Values.ToList());
        }

        [Fact]
        public async Task Announce_Election_WithoutEnough_Token()
        {
            // The starter transfer not enough token 
            var candidateInformation = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInformation, 50_000L);
            var balance = await Starter.GetBalanceAsync(candidateInformation);
            balance.ShouldBe(50_000L);

            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInformation);
            var result = await candidate.AnnounceElectionAsync("AElfin");
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient balance").ShouldBeTrue();
            var candidatesList = await candidate.GetCandidatesListAsync();
            candidatesList.Values.ToList().Contains(candidateInformation).ShouldBeFalse();
        }

        [Fact]
        public async Task Announce_Election_Twice()
        {
            // The starter transfer 200_000L
            var candidateInfo = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection * 2);

            // Check balance.
            {
                var balance = await Starter.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(DPoSContractConsts.LockTokenForElection * 2);
            }

            var candidate = Starter.CreateNewContractTester(candidateInfo);

            // Announce election.
            {
                var result = await candidate.AnnounceElectionAsync("AElfin");
                result.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }
            
            // Check balance.
            {
                var balance = await Starter.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(DPoSContractConsts.LockTokenForElection);
            }

            // Announce election again.
            {
                var result = await candidate.AnnounceElectionAsync("AElfinAgain");
                result.Status.ShouldBe(TransactionResultStatus.Failed);
            }

            // Check candidate list again.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Quit_Election_Success()
        {
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateInfo = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);

            // Check balance.
            {
                var balance = await Starter.GetBalanceAsync(candidateInfo);
                Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);
            }

            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInfo);

            await candidate.AnnounceElectionAsync("AElfin");

            // Check balance.
            {
                var balance = await candidate.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(0L);
            }

            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.ToList().Contains(candidateInfo).ShouldBeTrue();
            }

            // Quit election
            var result = await candidate.QuitElectionAsync();
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check candidates list.
            {
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.Contains(candidateInfo).ShouldBeFalse();
            }

            // Check balance.
            {
                var balance = await candidate.GetBalanceAsync(candidateInfo);
                balance.ShouldBe(DPoSContractConsts.LockTokenForElection);
            }
        }

        [Fact]
        public async Task Quit_Election_NoOneAnnounce()
        {
            var candidateInfo = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection);

            // Didn't announce election, but call quit announce.
            candidateInfo = GenerateNewUser();
            var notCandidate = Starter.CreateNewContractTester(candidateInfo);
            var result = await notCandidate.QuitElectionAsync();
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidField]).ShouldBeTrue();

            balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(0L);
        }

        [Fact]
        public async Task Quit_Election_WithoutAnnounce()
        {
            var candidateInfo = GenerateNewUser();
            await Starter.IssueTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection);

            await Starter.GenerateCandidatesAsync(1);

            // Didn't announce election, but call quit announce.
            candidateInfo = GenerateNewUser();
            var notCandidate = Starter.CreateNewContractTester(candidateInfo);
            var result = await notCandidate.QuitElectionAsync();
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();

            balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(0L);
        }

        [Fact]
        public async Task Vote_Candidate_Success()
        {
            const long amount = 1000;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), 10000);

            await voter.Vote(candidate.PublicKey, amount, 100);

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfCandidate.ObtainedTickets);

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfVoter.VotedTickets);
        }

        [Fact]
        public async Task Vote_Not_Candidate()
        {
            const long amount = 1000;
            await Starter.GenerateCandidatesAsync(1);
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), amount);

            var notCandidate = GenerateNewUser();
            var result = await voter.Vote(notCandidate, amount, 100);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(amount);
        }

        [Fact]
        public async Task Vote_Candidate_Without_Enough_Token()
        {
            const long amount = 100;
            const long voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), amount);

            var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Insufficient balance.").ShouldBeTrue();

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(0L);
        }

        [Fact]
        public async Task Vote_Same_Candidate_MultipleTimes()
        {
            const long amount = 1000;
            const long voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), amount);

            for (int i = 0; i < 5; i++)
            {
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(1000L);
            var ticketsCount = (await voter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsCount))).DeserializeToInt64();
            ticketsCount.ShouldBe(1000L);
            var votesCount = (await voter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetVotesCount))).DeserializeToInt64();
            votesCount.ShouldBe(5L);

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(0L);
        }

        [Fact]
        public async Task Vote_Different_Candidates()
        {
            const long amount = 1000;
            const long voteAmount = 200;
            var candidateLists = await Starter.GenerateCandidatesAsync(5);

            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), amount);

            for (int i = 0; i < 5; i++)
            {
                var candidate = candidateLists[i];
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(1000L);

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(0L);
        }

        [Fact]
        public async Task Vote_Lock_Days_Illegal()
        {
            const long amount = 1000;
            const long voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), amount);

            var txResult1 = await voter.Vote(candidate.PublicKey, voteAmount, 89);
            txResult1.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult1.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();

            var txResult2 = await voter.Vote(candidate.PublicKey, voteAmount, 1096);
            txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult2.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();
        }

        [Fact]
        public async Task Vote_By_Candidate()
        {
            const long amount = 1000;
            const long voteAmount = 200;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            await Starter.IssueTokenAsync(candidate.GetCallOwnerAddress(), amount);

            var txResult = await candidate.Vote(candidate.PublicKey, voteAmount, 90);
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();
        }

        [Fact]
        public async Task IsCandidate_Success()
        {
            var candidateLists = await Starter.GenerateCandidatesAsync(2);
            var nonCandidateInfo = GenerateNewUser();
            var candidate = Starter.CreateNewContractTester(nonCandidateInfo.KeyPair);
            var candidateResult = await candidate.CallContractMethodAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.IsCandidate), nonCandidateInfo.PublicKey);
            candidateResult.DeserializeToBool().ShouldBeFalse();

            var candidateResult1 = await candidate.CallContractMethodAsync(candidate.GetConsensusContractAddress(),
                nameof(ConsensusContract.IsCandidate), candidateLists[0].PublicKey);
            candidateResult1.DeserializeToBool().ShouldBeTrue();
        }

        [Fact]
        public async Task Withdraw_By_TransactionId()
        {
            const long amount = 1000;
            const int lockTime = 100;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voters = await Starter.GenerateVotersAsync(2);

            foreach (var voter in voters)
            {
                await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), 10000);
            }
            
            var txResult = await voters[0].Vote(candidate.PublicKey, amount, lockTime);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var blockAge = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge))).DeserializeToInt64();

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            ticketsOfCandidate.ObtainedTickets.ShouldBe(amount);
            var ticketsOfVoter = await voters[0].GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(amount);

            //Change term
            await Miners.ChangeTermAsync(1);

            // UnlockAge > CurrentAge
            string txId = txResult.ReadableReturnValue;
            var withdrawResult1 = await voters[0]
                .ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawByTransactionId), txId);
            withdrawResult1.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult1.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation])
                .ShouldBeTrue();

            await Starter.SetBlockchainAgeAsync(blockAge + lockTime);

            //Withdraw by other voter
            var withdrawResult2 = await voters[1]
                .ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawByTransactionId), txId);
            withdrawResult2.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult2.Error.Contains(ContractErrorCode.Message[ContractErrorCode.NoPermission]).ShouldBeTrue();

            //Withdrawn 
            var withdrawResult3 = await voters[0]
                .ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawByTransactionId), txId);
            withdrawResult3.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check withdrawal time and unlock age
            var votingRecord = await voters[0].GetVotingRecord(txResult.TransactionId);
            votingRecord.UnlockAge.ShouldBe(blockAge + lockTime);

            //Check candidate & voter tickets
            ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            ticketsOfCandidate.ObtainedTickets.ShouldBe(0L);
            ticketsOfVoter = await voters[0].GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(0L);

            //Check voter balance
            var balanceResult = await Starter.GetBalanceAsync(voters[0].GetCallOwnerAddress());
            balanceResult.ShouldBe(10000L);

            //Has already withdrawn
            var withdrawResult4 = await voters[0]
                .ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawByTransactionId), txId);
            withdrawResult4.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult4.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation])
                .ShouldBeTrue();
        }

        [Fact]
        public async Task Withdraw_All()
        {
            const long amount = 1000;
            var lockTimes = new List<int> {90, 180, 365, 730, 1095};

            var candidateLists = await Starter.GenerateCandidatesAsync(5);
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.IssueTokenAsync(voter.GetCallOwnerAddress(), 10000);
            var txResultList = new List<TransactionResult>();
            var votingRecordList = new List<VotingRecord>();

            for (int i = 0; i < 5; i++)
            {
                var candidate = candidateLists[i];
                var txResult = await voter.Vote(candidate.PublicKey, amount, lockTimes[i]);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResultList.Add(txResult);

                var votingRecord = await voter.GetVotingRecord(txResultList[i].TransactionId);
                votingRecordList.Add(votingRecord);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(5000L);

            //Get vote block age 
            var blockAge = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge))).DeserializeToInt64();

            //Change term and get term weights
            await Miners.ChangeTermAsync(MiningInterval);
            var currentTermNumber = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber))).DeserializeToInt64();
            var termTotalWeights = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetTermTotalWeights), currentTermNumber)).DeserializeToInt64();
            termTotalWeights.ShouldBe(votingRecordList[0].Weight + votingRecordList[1].Weight +
                                      votingRecordList[2].Weight + votingRecordList[3].Weight +
                                      votingRecordList[4].Weight);

            //Change term and set block age
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(blockAge + 365);

            //Check duration day 
            var getDurationDays1 = (await voter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), votingRecordList[0], blockAge + 365)).DeserializeToInt64();
            getDurationDays1.ShouldBe((long)lockTimes[0]);
            
            var getDurationDays2 = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), votingRecordList[3], blockAge + 365)).DeserializeToInt64();
            getDurationDays2.ShouldBe(blockAge + 365);
            
            //Withdraw all 
            var withdrawResult =
                await voter.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawAll));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check candidate & voter tickets
            ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(2000L);

            //Check withdraw transactions
            for (int i = 0; i < 5; i++)
            {   
                var votingRecord = await voter.GetVotingRecord(txResultList[i].TransactionId);
                if (i <= 2)
                    votingRecord.IsWithdrawn.ShouldBeTrue();
                else
                    votingRecord.IsWithdrawn.ShouldBeFalse();
            }

            //Check weight
            var currentTermNumber1 = (await voter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber))).DeserializeToInt64();
            var termTotalWeights1 = (await voter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetTermTotalWeights), currentTermNumber1)).DeserializeToInt64();
            termTotalWeights1.ShouldBeLessThan(termTotalWeights);
        }

        private static User GenerateNewUser()
        {
            var callKeyPair = CryptoHelpers.GenerateKeyPair();
            var callAddress = Address.FromPublicKey(callKeyPair.PublicKey);
            var callPublicKey = callKeyPair.PublicKey.ToHex();

            return new User
            {
                KeyPair = callKeyPair,
                Address = callAddress,
                PublicKey = callPublicKey
            };
        }

        private struct User
        {
            public ECKeyPair KeyPair { get; set; }
            public Address Address { get; set; }
            public string PublicKey { get; set; }

            public static implicit operator ECKeyPair(User user)
            {
                return user.KeyPair;
            }

            public static implicit operator Address(User user)
            {
                return user.Address;
            }

            public static implicit operator string(User user)
            {
                return user.PublicKey;
            }
        }
    }
}