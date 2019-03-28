using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ViewTest
    {
        public ViewTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval));
            Miners = Enumerable.Range(0, MinersCount)
                .Select(i => Starter.CreateNewContractTester(minersKeyPairs[i])).ToList();
        }

        public readonly ContractTester<DPoSContractTestAElfModule> Starter;

        private const int MinersCount = 3;

        private const int MiningInterval = 4000;

        private readonly List<ContractTester<DPoSContractTestAElfModule>> Miners;

        private List<VotingRecord> _votingRecordList;
        private List<ContractTester<DPoSContractTestAElfModule>> _voterList;
        private List<ContractTester<DPoSContractTestAElfModule>> _candidateLists;

        private List<int> _lockTimes;
        private long _blockAge;
        private const long Amount = 1000;

        private async Task Vote()
        {
            _lockTimes = new List<int> {90, 180, 730};
            _votingRecordList = new List<VotingRecord>();
            _candidateLists = await Starter.GenerateCandidatesAsync(3);
            _voterList = await Starter.GenerateVotersAsync(3);

            for (var i = 0; i < _voterList.Count; i++)
            {
                await Starter.TransferTokenAsync(_voterList[i].GetCallOwnerAddress(), 100000);

                for (var j = 0; j < _candidateLists.Count; j++)
                {
                    var txResult = await _voterList[i].Vote(_candidateLists[i].PublicKey, Amount, _lockTimes[j]);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var votingRecord = await _voterList[i].GetVotingRecord(txResult.TransactionId);
                    _votingRecordList.Add(votingRecord);
                }
            }

            await Miners.RunConsensusAsync(1, true);
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
            await Miners.RunConsensusAsync(1, true);

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
                await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                    nameof(DividendContract.GetLatestRequestDividendsTermNumber), _votingRecordList[0])
            ).Value;
            latestRequestDividendsTermNumber.ShouldBe(_votingRecordList[0].TermNumber);

            // Get previous term Dividends
            var getTermDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermDividends),
                new SInt64Value
                {
                    Value = previousTermNumber
                })).Value;
            getTermDividends.ShouldBeGreaterThan(0L);

            // Check Dividends
            var termTotalWeights = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetTermTotalWeights),
                new SInt64Value
                {
                    Value = previousTermNumber
                })).Value;
            var checkDividends = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.CheckDividends),
                new CheckDividendsInput
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
                new CheckDividendsInput
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
                new CheckDividendsInput
                {
                    TicketsAmount = Amount,
                    LockTime = _lockTimes[0],
                    TermNumber = previousTermNumber + 1
                })).Value;
            checkDividendsError.ShouldBe(0L);
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
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(_blockAge + 180);

            //Check duration day 
            var getDurationDays1 = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetDurationDays),
                new VoteInfo
                {
                    Record = _votingRecordList[0],
                    Age = _blockAge + 180
                })).Value;
            getDurationDays1.ShouldBe(_lockTimes[0]);

            var getDurationDays2 = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetDurationDays),
                new VoteInfo
                {
                    Record = _votingRecordList[2],
                    Age = _blockAge + 180
                })).Value;
            getDurationDays2.ShouldBe(_blockAge + 180);

            //GetExpireTermNumber
            var expireTermNumber = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendContract.GetExpireTermNumber),
                new VoteInfo
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
                new PublicKey
                {
                    Hex = _candidateLists[0].PublicKey
                }
            )).Value;
            notExpireVotes.ShouldBe(1000L);

            //QueryObtainedVotes
            var obtainedVotes = SInt64Value.Parser.ParseFrom(await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedVotes),
                new PublicKey
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
    }
}