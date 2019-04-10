using System;
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
        private readonly ContractTester<DPoSContractTestAElfModule> Starter;

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

            MinersKeyPairs = Enumerable.Range(0, MinersCount - 1).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            // Enable Start to use SetBlockchainAge method.
            MinersKeyPairs.Add(Starter.KeyPair);
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(MinersKeyPairs));
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
                var round = Round.Parser.ParseFrom(roundInformation);
                round.ShouldBe(new Round());
            }

            //query with result
            {
                var input = new SInt64Value
                {
                    Value = 1
                };
                var roundInformation = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetRoundInformation), input);
                var round = Round.Parser.ParseFrom(roundInformation);
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
            var roundNumber = SInt64Value.Parser.ParseFrom(roundInformation);
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
            historyInfo.Terms.Count.ShouldBe(1);
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
            historyDictionaryInfo.CandidatesNumber.ShouldBe(MinersCount);
            historyDictionaryInfo.Maps.Count.ShouldBe(input.Length);

            input.Start = 1;
            bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableCandidatesHistoryInfo), input);
            historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.CandidatesNumber.ShouldBe(MinersCount);
            historyDictionaryInfo.Maps.Count.ShouldBeGreaterThanOrEqualTo(input.Length);

            input.Start = 2;
            bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableCandidatesHistoryInfo), input);
            historyDictionaryInfo = CandidateInHistoryDictionary.Parser.ParseFrom(bytes);
            historyDictionaryInfo.CandidatesNumber.ShouldBe(MinersCount);
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
        public async Task GetPageableTicketsHistories()
        {
            var candidates = await PrepareConsensusWithCandidateEnvironment();

            var input = new PageableTicketsInfoInput
            {
                Length = 2,
                PublicKey = candidates[0].PublicKey,
                Start = 0
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetPageableTicketsHistories), input);
            var ticketsInfo = TicketsHistories.Parser.ParseFrom(bytes);
            ticketsInfo.Values.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetPageableElectionInfo()
        {
            await PrepareConsensusWithCandidateEnvironment();

            var input = new PageableElectionInfoInput
            {
                Length = 3,
                OrderBy = 0,
                Start = 0
            };

            //OrderBy = 0 default
            {
                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(MinersCount);
            }

            //OrderBy = 1  ascending
            {
                input.OrderBy = 1;

                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(MinersCount);
                List<long> tickets = new List<long>();
                foreach (var electionInfo in electionsInfo.Maps.Values)
                {
                    tickets.Add(electionInfo.ObtainedTickets);
                }

                tickets[0].ShouldBeLessThanOrEqualTo(tickets[1]);
                tickets[1].ShouldBeLessThanOrEqualTo(tickets[2]);
            }

            //OrderBy = 2 descending
            {
                input.OrderBy = 2;

                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(MinersCount);
                List<long> tickets = new List<long>();
                foreach (var electionInfo in electionsInfo.Maps.Values)
                {
                    tickets.Add(electionInfo.ObtainedTickets);
                }

                tickets[0].ShouldBeGreaterThanOrEqualTo(tickets[1]);
                tickets[1].ShouldBeGreaterThanOrEqualTo(tickets[2]);
            }
            //OrderBy = others
            {
                input.OrderBy = 10;

                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(0);
            }
            //Page count test
            {
                input = new PageableElectionInfoInput
                {
                    Start = 0,
                    Length = 2,
                    OrderBy = 0
                };
                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(2);
            }
            //Page index test
            {
                input = new PageableElectionInfoInput
                {
                    Start = 2,
                    Length = 3,
                    OrderBy = 0
                };
                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetPageableElectionInfo), input);
                var electionsInfo = TicketsDictionary.Parser.ParseFrom(bytes);
                electionsInfo.Maps.Count.ShouldBe(1);
            }
        }

        [Fact]
        public async Task GetCurrentVictories()
        {
            var candidates = await PrepareConsensusWithCandidateEnvironment();

            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentVictories), new Empty());
            var publicKeys = StringList.Parser.ParseFrom(bytes);
            publicKeys.Values.Count.ShouldBe(MinersCount);
            publicKeys.Values.Contains(candidates[0].PublicKey).ShouldBeTrue();
            publicKeys.Values.Contains(candidates[1].PublicKey).ShouldBeTrue();
            publicKeys.Values.Contains(candidates[2].PublicKey).ShouldBeTrue();
        }

        [Fact]
        public async Task GetTermSnapshot()
        {
            await PrepareConsensusWithCandidateEnvironment();

            var input = new SInt64Value
            {
                Value = 1
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTermSnapshot), input);
            var termSnapshot = TermSnapshot.Parser.ParseFrom(bytes);
            termSnapshot.TermNumber.ShouldBe(input.Value);
            termSnapshot.EndRoundNumber.ShouldBe(input.Value + 1);
            termSnapshot.CandidatesSnapshot.Count.ShouldBe(MinersCount);
        }

        [Fact]
        public async Task QueryAlias()
        {
            var candidates = await PrepareConsensusWithCandidateEnvironment();

            var input = new PublicKey
            {
                Hex = candidates[0].PublicKey
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryAlias), input);
            var alias = Alias.Parser.ParseFrom(bytes);
            alias.Value.ShouldBe(candidates[0].PublicKey.Substring(0, DPoSContractConsts.AliasLimit));
        }

        [Fact(Skip = "Not implemented talked with Yiqi.")]
        public async Task GetTermNumberByRoundNumber()
        {
            await PrepareConsensusWithCandidateEnvironment();

            var input = new SInt64Value()
            {
                Value = 1
            };
            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTermNumberByRoundNumber), input);
            var termNumber = SInt64Value.Parser.ParseFrom(bytes).Value;
            termNumber.ShouldBe(0L);

            var roundBytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentRoundNumber), new Empty());
            var currentRoundNumber = SInt64Value.Parser.ParseFrom(roundBytes).Value;

            var termBytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber), new Empty());
            var currentTermNumber = SInt64Value.Parser.ParseFrom(termBytes).Value;

            input.Value = currentRoundNumber;
            bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetTermNumberByRoundNumber), input);
            termNumber = SInt64Value.Parser.ParseFrom(bytes).Value;
            termNumber.ShouldBe(currentTermNumber);
        }

        [Fact]
        public async Task QueryAliasesInUse()
        {
            await PrepareConsensusWithCandidateEnvironment();

            var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryAliasesInUse), new Empty());
            var stringList = StringList.Parser.ParseFrom(bytes);

            stringList.Values.Count.ShouldBe(3);
        }

        [Fact]
        public async Task QueryMinedBlockCountInCurrentTerm()
        {
            await PrepareConsensusWithCandidateEnvironment();

            SInt64Value count = new SInt64Value
            {
                Value = 0
            };
            foreach (var bpInfo in MinersKeyPairs)
            {
                var input = new PublicKey
                {
                    Hex = bpInfo.PublicKey.ToHex()
                };
                var bytes = await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.QueryMinedBlockCountInCurrentTerm), input);
                count = SInt64Value.Parser.ParseFrom(bytes);

                if (count.Value > 0)
                    break;
            }

            count.Value.ShouldBeGreaterThanOrEqualTo(1L);
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
            await Starter.SetBlockchainAgeAsync(_blockAge + 200);

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

        private async Task<List<ContractTester<DPoSContractTestAElfModule>>> PrepareConsensusWithCandidateEnvironment()
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
                        Amount = new Random(DateTime.Now.Millisecond).Next(50, 100),
                        LockTime = new Random(DateTime.Now.Millisecond).Next(10, 100) * 10
                    }));
            }

            await MinerList.MineAsync(voteTxs);
            await MinerList.RunConsensusAsync(1, true);

            return candidates;
        }
    }
}