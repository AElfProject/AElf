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
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ViewTest
    {
        public readonly ContractTester<DPoSContractTestAElfModule> Starter;

        private const int MinersCount = 3;

        private const int MiningInterval = 4000;

        private readonly List<ContractTester<DPoSContractTestAElfModule>> MinerList;
        private readonly List<ECKeyPair> MinersKeyPairs;

        private List<VotingRecord> _votingRecordList;
        private List<ContractTester<DPoSContractTestAElfModule>> _voterList;
        private List<ContractTester<DPoSContractTestAElfModule>> _candidateLists;

        private List<int> _lockTimes;
        private long _blockAge;
        private const long Amount = 1000;

        public ViewTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            MinersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(MinersKeyPairs, MiningInterval));
            MinerList = Enumerable.Range(0, MinersCount)
                .Select(i => Starter.CreateNewContractTester(MinersKeyPairs[i])).ToList();
        }

        [Fact]
        public async Task Get_RoundInformation()
        {
            //query return null
            {
                var input = new SInt64Value
                {
                    Value = 2
                };
                var roundInformation = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetRoundInformation), input);
                var round = roundInformation.DeserializeToPbMessage<Round>();
                round.ShouldBeNull();
            }

            //query with result
            {
                var input = new SInt64Value
                {
                    Value = 1
                };
                var roundInformation = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetRoundInformation), input);
                var round = roundInformation.DeserializeToPbMessage<Round>();
                round.ShouldNotBeNull();
                round.RoundNumber.ShouldBe(1);
                round.RealTimeMinersInformation.Count.ShouldBe(3);
            }
        }

        [Fact]
        public async Task Get_CurrentRoundInformation()
        {
            var roundInformation = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentRoundNumber), new Empty());
            var roundNumber = roundInformation.DeserializeToPbMessage<SInt64Value>();
            roundNumber.ShouldNotBeNull();
            roundNumber.Value.ShouldBe(1);
        }

        [Fact]
        public async Task GetCandidateList_Success()
        {
            //no candidate
            {
                var candidates = await Starter.GetCandidatesListAsync();
                candidates.Values.Count.ShouldBe(0);
            }

            //with candidate
            {
                var candidateInformation = TestUserHelper.GenerateNewUser();
                await Starter.TransferTokenAsync(candidateInformation, DPoSContractConsts.LockTokenForElection);
                var balance = await Starter.GetBalanceAsync(candidateInformation);
                Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);

                // The candidate announce election.
                var candidate = Starter.CreateNewContractTester(candidateInformation);
                await candidate.AnnounceElectionAsync("AElfin");

                //Assert
                var candidatesList = await candidate.GetCandidatesListAsync();
                candidatesList.Values.Count.ShouldBe(1);
            }
        }

        [Fact]
        public async Task GetCandidates_Success()
        {
            //no candidate
            {
                var candidates = await Starter.GetCandidatesAsync();
                candidates.PublicKeys.Count.ShouldBe(0);
                candidates.Addresses.Count.ShouldBe(0);
            }

            //with candidate
            {
                var candidateInformation = TestUserHelper.GenerateNewUser();
                await Starter.TransferTokenAsync(candidateInformation, DPoSContractConsts.LockTokenForElection);
                var balance = await Starter.GetBalanceAsync(candidateInformation);
                Assert.Equal(DPoSContractConsts.LockTokenForElection, balance);

                // The candidate announce election.
                var candidate = Starter.CreateNewContractTester(candidateInformation);
                await candidate.AnnounceElectionAsync("AElfin");

                //Assert
                var candidates = await candidate.GetCandidatesAsync();
                candidates.PublicKeys.Count.ShouldBe(1);
                candidates.Addresses.Count.ShouldBe(1);
                candidates.IsInitialMiners.ShouldBeFalse();
            }
        }

        [Fact]
        public async Task GetCandidateHistoryInformation_WithNot_CandidateUser()
        {
            var input = new PublicKey()
            {
                Hex = Starter.PublicKey
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidateHistoryInformation), input);
            var historyInfo = CandidateInHistory.Parser.ParseFrom(bytes);
            historyInfo.PublicKey.ShouldBe(Starter.PublicKey);
            historyInfo.ContinualAppointmentCount.ShouldBe(0);
            historyInfo.MissedTimeSlots.ShouldBe(0);
            historyInfo.ProducedBlocks.ShouldBe(0);
            historyInfo.ReappointmentCount.ShouldBe(0);
        }

        [Fact]
        public async Task GetCandidateHistoryInformation__With_CandidateUser()
        {
            await PrepareConsensusWithCandidateEnvironment();

            //Action
            var input = new PublicKey()
            {
                Hex = MinersKeyPairs[0].PublicKey.ToHex()
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidateHistoryInformation), input);
            var historyInfo = CandidateInHistory.Parser.ParseFrom(bytes);

            //Assert
            historyInfo.PublicKey.ShouldBe(input.Hex);
            historyInfo.ProducedBlocks.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Get_CandidateInHistoryDictionary_Without_Candidate()
        {
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidatesHistoryInfo), new Empty());
            var historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.Maps.Count.ShouldBe(0);
            historyDictionaryInfo.CandidatesNumber.ShouldBe(0);
        }
        
        [Fact]
        public async Task Get_CandidateInHistoryDictionary_With_Candidate()
        {
            await PrepareConsensusWithCandidateEnvironment();
            
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCandidatesHistoryInfo), new Empty());
            var historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.CandidatesNumber.ShouldBeGreaterThanOrEqualTo(MinersCount);
            historyDictionaryInfo.Maps.Count.ShouldBeGreaterThanOrEqualTo(MinersCount);
        }

        [Fact]
        public async Task GetPageableCandidatesHistoryInfo_Success()
        {
            await PrepareConsensusWithCandidateEnvironment();
            
            var input = new PageInfo()
            {
                Length = 2,
                Start = 0
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableCandidatesHistoryInfo), input);
            var historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.CandidatesNumber.ShouldBeGreaterThanOrEqualTo(input.Length);
            historyDictionaryInfo.Maps.Count.ShouldBeGreaterThanOrEqualTo(input.Length);

            input.Start = 1;
            bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableCandidatesHistoryInfo), input);
            historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.CandidatesNumber.ShouldBeGreaterThanOrEqualTo(1);
            historyDictionaryInfo.Maps.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetCurrentMiners_Success()
        {
            await PrepareConsensusWithCandidateEnvironment();

            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentMiners), new Empty());
            var minersInfo = Miners.Parser.ParseFrom(bytes);
            minersInfo.PublicKeys.Count.ShouldBe(3);
            minersInfo.Addresses.Count.ShouldBe(3);
            minersInfo.PublicKeys.Contains(MinersKeyPairs[0].PublicKey.ToHex()).ShouldBeTrue();
            minersInfo.Addresses.Contains(Address.FromPublicKey(MinersKeyPairs[0].PublicKey)).ShouldBeTrue();
        }
        
        [Fact]
        public async Task Query_Tickets_Info()
        {
            await Vote();

            // Change the block age 
            _blockAge = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge),
                new Empty())).Value;
            await MinerList.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(_blockAge + 180);

            //Check duration day 
            var getDurationDays1 = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetDurationDays),
                new VoteInfo()
                {
                    Record = _votingRecordList[0],
                    Age = _blockAge + 180
                })).Value;
            getDurationDays1.ShouldBe(_lockTimes[0]);

            var getDurationDays2 = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetDurationDays),
                new VoteInfo()
                {
                    Record = _votingRecordList[2],
                    Age = _blockAge + 180
                })).Value;
            getDurationDays2.ShouldBe(_blockAge + 180);

            //GetExpireTermNumber
            var expireTermNumber = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetExpireTermNumber),
                new VoteInfo()
                {
                    Record = _votingRecordList[0],
                    Age = _blockAge + 180
                })).Value;
            expireTermNumber.ShouldBe(_votingRecordList[0].TermNumber +
                                      getDurationDays1 / ConsensusDPoSConsts.DaysEachTerm);

            //QueryObtainedNotExpiredVotes
            var notExpireVotes = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedNotExpiredVotes),
                new PublicKey()
                {
                    Hex = _candidateLists[0].PublicKey
                }
            )).Value;
            notExpireVotes.ShouldBe(1000L);

            //QueryObtainedVotes
            var obtainedVotes = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedVotes),
                new PublicKey()
                {
                    Hex = _candidateLists[0].PublicKey
                }
            )).Value;
            obtainedVotes.ShouldBe(3000L);

            //GetTicketsInformation
            var voterTicketsInfo = await _voterList[0].GetTicketsInformationAsync();
            voterTicketsInfo.VotedTickets.ShouldBe(3000L);
            voterTicketsInfo.ObtainedTickets.ShouldBe(0L);
            voterTicketsInfo.VotingRecordsCount.ShouldBe(3L);
            voterTicketsInfo.VoteToTransactions.Count.ShouldBe(3);

            var pageableTicketsInfo = await Starter.GetPageableTicketsInfo(_voterList[0].PublicKey, 0, 0);
            voterTicketsInfo.ObtainedTickets.ShouldBe(pageableTicketsInfo.ObtainedTickets);
            voterTicketsInfo.VotedTickets.ShouldBe(pageableTicketsInfo.VotedTickets);
            voterTicketsInfo.VotingRecordsCount.ShouldBe(pageableTicketsInfo.VoteToTransactions.Count);
            voterTicketsInfo.HistoryObtainedTickets.ShouldBe(pageableTicketsInfo.HistoryObtainedTickets);

            //Withdraw all
            var withdrawResult =
                await _voterList[0]
                    .ExecuteConsensusContractMethodWithMiningAsync(
                        nameof(ConsensusContract.WithdrawAll),
                        new Empty());
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //GetPageableNotWithdrawnTicketsInfo
            var pageAbleNotWithdrawnTicketsInfo =
                await Starter.GetPageableNotWithdrawnTicketsInfo(_voterList[0].PublicKey, 0, 0);
            pageAbleNotWithdrawnTicketsInfo.VotedTickets.ShouldBe(1000L);
            pageAbleNotWithdrawnTicketsInfo.VotingRecordsCount.ShouldBe(1L);
        }

        [Fact]
        public async Task Query_Dividends_Info()
        {
            await Vote();

            var previousTermNumber = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber),
                new Empty())).Value;

            // Change term
            await MinerList.RunConsensusAsync(1, true);

            //Query dividends
            var queryCurrentDividendsForVoters = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryCurrentDividendsForVoters),
                new Empty()
            )).Value;
            queryCurrentDividendsForVoters.ShouldBe((long) (DPoSContractConsts.ElfTokenPerBlock * 0.2));

            var queryCurrentDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryCurrentDividends),
                new Empty()
            )).Value;
            queryCurrentDividends.ShouldBe(DPoSContractConsts.ElfTokenPerBlock);

            //Get latest request dividends term number
            var latestRequestDividendsTermNumber = SInt64Value.Parser.ParseFrom(
                (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                    nameof(DividendContract.GetLatestRequestDividendsTermNumber), _votingRecordList[0]))
            ).Value;
            latestRequestDividendsTermNumber.ShouldBe(_votingRecordList[0].TermNumber);

            // Get previous term Dividends
            var getTermDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermDividends),
                new SInt64Value()
                {
                    Value = previousTermNumber
                })).Value;
            getTermDividends.ShouldBeGreaterThan(0L);

            // Check Dividends
            var termTotalWeights = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermTotalWeights),
                new SInt64Value()
                {
                    Value = previousTermNumber
                })).Value;
            var checkDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.CheckDividends),
                new CheckDividendsInput()
                {
                    TicketsAmount = Amount,
                    LockTime = _lockTimes[0],
                    TermNumber = previousTermNumber
                })).Value;
            var dividends = _votingRecordList[0].Weight * getTermDividends / termTotalWeights;
            checkDividends.ShouldBe(dividends);

            // Check Previous Term Dividends
            var votingGains = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.CheckDividends),
                new CheckDividendsInput()
                {
                    TicketsAmount = 10000,
                    LockTime = 180,
                    TermNumber = previousTermNumber
                })).Value;
            var checkPreviousTermDividends = await Starter.CheckDividendsOfPreviousTerm();
            checkPreviousTermDividends.Values[1].ShouldBe(votingGains);

            //Check the next term dividends
            var checkDividendsError = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.CheckDividends),
                new CheckDividendsInput()
                {
                    TicketsAmount = Amount,
                    LockTime = _lockTimes[0],
                    TermNumber = previousTermNumber + 1
                })).Value;
            checkDividendsError.ShouldBe(0L);
        }

        private async Task Vote()
        {
            _lockTimes = new List<int> {90, 180, 730};
            _votingRecordList = new List<VotingRecord>();
            _candidateLists = await Starter.GenerateCandidatesAsync(3);
            _voterList = await Starter.GenerateVotersAsync(3);

            for (int i = 0; i < _voterList.Count; i++)
            {
                await Starter.TransferTokenAsync(_voterList[i].GetCallOwnerAddress(), 100000);

                for (int j = 0; j < _candidateLists.Count; j++)
                {
                    var txResult = await _voterList[i].Vote(_candidateLists[i].PublicKey, Amount, _lockTimes[j]);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var votingRecord = await _voterList[i].GetVotingRecord(txResult.TransactionId);
                    _votingRecordList.Add(votingRecord);
                }
            }

            await MinerList.RunConsensusAsync(1, true);
        }

        private async Task PrepareConsensusWithCandidateEnvironment()
        {
            //Prepare env
            var voter = (await Starter.GenerateVotersAsync()).AnyOne();
            var candidates = await Starter.GenerateCandidatesAsync(MinersCount);

            //vote to candidates.
            var voteTxs = new List<Transaction>();
            foreach (var candidate in candidates)
            {
                voteTxs.Add(await voter.GenerateTransactionAsync(
                    Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.Vote),
                    new VoteInput()
                    {
                        CandidatePublicKey = candidate.PublicKey,
                        Amount = 1,
                        LockTime = 100
                    }));
            }

            await MinerList.MineAsync(voteTxs);
            await MinerList.RunConsensusAsync(1, true);
        }
    }
}