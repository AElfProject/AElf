using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
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

        private const long Amount = 1000;

        public ElectionTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, MinersCount - 1).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            minersKeyPairs.Add(Starter.KeyPair);
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval));
            Miners = Enumerable.Range(0, MinersCount)
                .Select(i => Starter.CreateNewContractTester(minersKeyPairs[i])).ToList();
        }

        [Fact]
        public async Task Announce_Election_Success()
        {
            // The starter transfer a specific amount of tokens to candidate for further testing.
            var candidateInformation = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInformation, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInformation);
            Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);

            // The candidate announce election.
            var candidate = Starter.CreateNewContractTester(candidateInformation);
            var result = await candidate.AnnounceElectionAsync("AElfin");
            var candidatesList = await candidate.GetCandidatesListAsync();

            // Check the candidates list.
            Assert.Contains(candidate.KeyPair.PublicKey.ToHex(), candidatesList.Values.ToList());
        }

        [Fact]
        public async Task Announce_Election_WithoutEnough_Token()
        {
            // The starter transfer not enough token 
            var candidateInformation = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInformation, 50_000L);
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
            var candidateInfo = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection * 2);

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
            var candidateInfo = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);

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
            var candidateInfo = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection);

            // Didn't announce election, but call quit announce.
            candidateInfo = TestUserHelper.GenerateNewUser();
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
            var candidateInfo = TestUserHelper.GenerateNewUser();
            await Starter.TransferTokenAsync(candidateInfo, DPoSContractConsts.LockTokenForElection);
            var balance = await Starter.GetBalanceAsync(candidateInfo);
            balance.ShouldBe(DPoSContractConsts.LockTokenForElection);

            await Starter.GenerateCandidatesAsync(1);

            // Didn't announce election, but call quit announce.
            candidateInfo = TestUserHelper.GenerateNewUser();
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
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), 10000);

            await voter.Vote(candidate.PublicKey, amount, 100);

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfCandidate.ObtainedTickets);

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            Assert.Equal(amount, ticketsOfVoter.VotedTickets);
        }

        [Fact]
        public async Task Vote_Not_Candidate()
        {
            const long pocketMoney = 10000;
            const long amount = 10000;
            await Starter.GenerateCandidatesAsync(1);
            var voter = (await Starter.GenerateVotersAsync(1, pocketMoney)).AnyOne();

            var notCandidate = TestUserHelper.GenerateNewUser();
            var result = await voter.Vote(notCandidate, amount, 100);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(amount);
        }

        [Fact]
        public async Task Vote_Candidate_Without_Enough_Token()
        {
            const long pocketMoney = 10000L;
            const long voteAmount = 200;
            
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();

            var txResult = await voter.Vote(candidate.PublicKey, pocketMoney + voteAmount, 100);
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Insufficient balance.").ShouldBeTrue();

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(0L);
        }

        [Fact]
        public async Task Vote_Same_Candidate_MultipleTimes()
        {
            const long pocketMoney = 10000L;
            const long voteAmount = 2000;
            const long votes = 5L;
            
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voter = (await Starter.GenerateVotersAsync(1, pocketMoney)).AnyOne();

            for (int i = 0; i < votes; i++)
            {
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(pocketMoney);
            var ticketsCount = SInt64Value.Parser.ParseFrom(await voter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsCount), new Empty())).Value;
            ticketsCount.ShouldBe(pocketMoney);
            var votesCount = SInt64Value.Parser.ParseFrom(await voter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetVotesCount), new Empty())).Value;
            votesCount.ShouldBe(votes);

            var balance = await Starter.GetBalanceAsync(voter.GetCallOwnerAddress());
            balance.ShouldBe(pocketMoney - voteAmount * votes);
        }

        [Fact]
        public async Task Vote_Different_Candidates()
        {
            const long pocketMoney = 10000L;
            const int votersCount = 5;
            var voteAmount = pocketMoney / votersCount;
            
            var candidateLists = await Starter.GenerateCandidatesAsync(5);

            var voter = (await Starter.GenerateVotersAsync(1, pocketMoney)).AnyOne();

            for (int i = 0; i < votersCount; i++)
            {
                var candidate = candidateLists[i];
                var txResult = await voter.Vote(candidate.PublicKey, voteAmount, 100);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(voteAmount * votersCount);

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
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), amount);

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
            await Starter.TransferTokenAsync(candidate.GetCallOwnerAddress(), amount);

            var txResult = await candidate.Vote(candidate.PublicKey, voteAmount, 90);
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains(ContractErrorCode.Message[ContractErrorCode.InvalidOperation]).ShouldBeTrue();
        }

        [Fact]
        public async Task IsCandidate_Success()
        {
            var candidateLists = await Starter.GenerateCandidatesAsync(2);
            var nonCandidateInfo = TestUserHelper.GenerateNewUser();
            var candidate = Starter.CreateNewContractTester(nonCandidateInfo.KeyPair);
            var candidateResult = Google.Protobuf.WellKnownTypes.BoolValue.Parser.ParseFrom(
                await candidate.CallContractMethodAsync(
                    candidate.GetConsensusContractAddress(),
                    nameof(ConsensusContract.IsCandidate),
                    new PublicKey() {Hex = nonCandidateInfo.PublicKey}));
            candidateResult.Value.ShouldBeFalse();

            var candidateResult1 = BoolValue.Parser.ParseFrom(
                await candidate.CallContractMethodAsync(candidate.GetConsensusContractAddress(),
                    nameof(ConsensusContract.IsCandidate),
                    new PublicKey() {Hex = candidateLists[0].PublicKey})).Value;
            candidateResult1.ShouldBeTrue();
        }

        [Fact]
        public async Task Withdraw_By_TransactionId()
        {
            const int lockTime = 100;
            const long pocketMoney = 10000L;
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voters = await Starter.GenerateVotersAsync(2, pocketMoney);

            var txResult = await voters[0].Vote(candidate.PublicKey, Amount, lockTime);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var blockAge = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge),
                new Empty())).Value;

            var ticketsOfCandidate = await candidate.GetTicketsInformationAsync();
            ticketsOfCandidate.ObtainedTickets.ShouldBe(Amount);
            var ticketsOfVoter = await voters[0].GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(Amount);

            //Change term
            await Miners.ChangeTermAsync(1);

            // UnlockAge > CurrentAge
            var txId = Hash.Parser.ParseFrom(txResult.ReturnValue);
            var withdrawResult1 = await voters[0]
                .ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.WithdrawByTransactionId), txId);
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
            var lockTimes = new List<int> {90, 180, 365, 730, 1095};

            var candidateLists = await Starter.GenerateCandidatesAsync(5);
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            await Starter.TransferTokenAsync(voter.GetCallOwnerAddress(), 10000);
            var txResultList = new List<TransactionResult>();
            var votingRecordList = new List<VotingRecord>();

            for (int i = 0; i < 5; i++)
            {
                var candidate = candidateLists[i];
                var txResult = await voter.Vote(candidate.PublicKey, Amount, lockTimes[i]);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResultList.Add(txResult);

                var votingRecord = await voter.GetVotingRecord(txResultList[i].TransactionId);
                votingRecordList.Add(votingRecord);
            }

            var ticketsOfVoter = await voter.GetTicketsInformationAsync();
            ticketsOfVoter.VotedTickets.ShouldBe(5000L);

            //Get vote block age 
            var blockAge = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge),
                new Empty())).Value;

            //Change term and get term weights
            await Miners.ChangeTermAsync(MiningInterval);
            var currentTermNumber = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber),
                new Empty())).Value;
            var termTotalWeights = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermTotalWeights),
                new SInt64Value(){Value = currentTermNumber})).Value;
            termTotalWeights.ShouldBe(votingRecordList[0].Weight + votingRecordList[1].Weight +
                                      votingRecordList[2].Weight + votingRecordList[3].Weight +
                                      votingRecordList[4].Weight);

            //Change term and set block age
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(blockAge + 365);

            //Withdraw all 
            var withdrawResult =
                await voter.ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.WithdrawAll), new Empty());
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
            var currentTermNumber1 = SInt64Value.Parser.ParseFrom(await voter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber),
                new Empty())).Value;
            var termTotalWeights1 = SInt64Value.Parser.ParseFrom(await voter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermTotalWeights),
                new SInt64Value(){Value =currentTermNumber1 })).Value;
            termTotalWeights1.ShouldBe(votingRecordList[3].Weight + votingRecordList[4].Weight);
        }

        [Fact]
        public async Task Receive_Dividends_By_TransactionId()
        {
            const int lockTime = 100;
            const long pocketMoney = 10000L;
            
            var candidate = (await Starter.GenerateCandidatesAsync(1))[0];
            var voterList = await Starter.GenerateVotersAsync(2, pocketMoney);

            // Check balance
            {
                var voteBalance = await Starter.GetBalanceAsync(Starter.GetAddress(voterList[0].KeyPair));
                voteBalance.ShouldBe(pocketMoney);
            }

            var txResult = await voterList[0].Vote(candidate.PublicKey, Amount, lockTime);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var ticketsCount = SInt64Value.Parser.ParseFrom(await voterList[0].CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsCount),
                new Empty())).Value;

            var txId = Hash.Parser.ParseFrom(txResult.ReturnValue);
            // Check balance
            {
                var voteBalance = await Starter.GetBalanceAsync(Starter.GetAddress(voterList[0].KeyPair));
                voteBalance.ShouldBe(pocketMoney - ticketsCount);
            }

            //Change term
            await Miners.ChangeTermAsync(MiningInterval);
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(10);

            //Receive by other voter
            var receiveErrorResult = await voterList[1]
                .ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.ReceiveDividendsByTransactionId), txId);
            receiveErrorResult.Status.ShouldBe(TransactionResultStatus.Failed);
            receiveErrorResult.Error.Contains(ContractErrorCode.Message[ContractErrorCode.NoPermission]).ShouldBeTrue();

            //Check dividends
            var votingRecord = await voterList[0].GetVotingRecord(txResult.TransactionId);
            var getAvailableDividends = SInt64Value.Parser.ParseFrom(
                await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetAvailableDividends), votingRecord)).Value;

            {
                var voteBalance = await Starter.GetBalanceAsync(Starter.GetAddress(voterList[0].KeyPair));
                voteBalance.ShouldBe(pocketMoney - ticketsCount);

                //Receive
                var receiveResult = await voterList[0]
                    .ExecuteConsensusContractMethodWithMiningAsync(
                        nameof(ConsensusContract.ReceiveDividendsByTransactionId), txId);
                receiveResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //Validation
                var voteBalanceAfterReceive = await Starter.GetBalanceAsync(Starter.GetAddress(voterList[0].KeyPair));
                var receiveDividends = voteBalanceAfterReceive - voteBalance;
                getAvailableDividends.ShouldBe(receiveDividends);
            }
        }

        [Fact]
        public async Task Receive_All_Dividends()
        {
            const long pocketMoney = 10000L;

            var lockTimes = new List<int> {90, 180, 365, 730, 1095};
            var candidateList = await Starter.GenerateCandidatesAsync(5);
            var voter = (await Starter.GenerateVotersAsync(1, pocketMoney)).AnyOne();
            var txResultList = new List<TransactionResult>();
            var votingRecordList = new List<VotingRecord>();
            var getAvailableDividendList = new List<long>();
            var availableDividends = 0L;

            for (int i = 0; i < candidateList.Count; i++)
            {
                var txResult = await voter.Vote(candidateList[i].PublicKey, Amount, lockTimes[i]);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResultList.Add(txResult);

                var votingRecord = await voter.GetVotingRecord(txResultList[i].TransactionId);
                votingRecordList.Add(votingRecord);
            }

            var ticketsCount = SInt64Value.Parser.ParseFrom(await voter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTicketsCount),
                new Empty())).Value;
            //Get vote block age 
            var blockAge = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge),
                new Empty())).Value;

            await Miners.ChangeTermAsync(MiningInterval);
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(blockAge + 91);

            for (var i = 0; i < votingRecordList.Count; i++)
            {
                var getAvailableDividend = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                    Starter.GetDividendsContractAddress(),
                    nameof(DividendContract.GetAvailableDividends),
                    votingRecordList[i])).Value;
                getAvailableDividendList.Add(getAvailableDividend);
                availableDividends += getAvailableDividendList[i];
            }
            
            var getAllAvailableDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetAllAvailableDividends),
                new PublicKey()
                {
                    Hex =voter.PublicKey 
                })).Value;
            getAllAvailableDividends.ShouldBe(availableDividends);

            var voteBalance = await Starter.GetBalanceAsync(Starter.GetAddress(voter.KeyPair));
            voteBalance.ShouldBe(pocketMoney - ticketsCount);

            var receiveResult =
                await voter.ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.ReceiveAllDividends), new Empty());
            receiveResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Validation
            var voteBalanceAfterReceive = await Starter.GetBalanceAsync(Starter.GetAddress(voter.KeyPair));
            var receiveDividends = voteBalanceAfterReceive - voteBalance;
            availableDividends.ShouldBe(receiveDividends);

            await Starter.SetBlockchainAgeAsync(blockAge + 100);
            var getAvailableDividend1 =SInt64Value.Parser.ParseFrom(
                await Starter.CallContractMethodAsync(
                    Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetAvailableDividends),
                    votingRecordList[0])
                ).Value;
            getAvailableDividend1.ShouldBe(0);
        }
    }
}