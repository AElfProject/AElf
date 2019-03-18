using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Org.BouncyCastle.Crypto.Engines;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using Xunit.Sdk;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ViewTest
    {
        public readonly ContractTester<DPoSContractTestAElfModule> Starter;

        private const int MinersCount = 3;

        private const int MiningInterval = 4000;

        private readonly List<ContractTester<DPoSContractTestAElfModule>> Miners;
        
        private List<VotingRecord> _votingRecordList ;
        private List<ContractTester<DPoSContractTestAElfModule>> _voterList;
        private List<ContractTester<DPoSContractTestAElfModule>> _candidateLists;

        private List<int>_lockTimes ;
        private long _blockAge;
        private const long Amount = 1000;

        public ViewTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval));
            Miners = Enumerable.Range(0, MinersCount)
                .Select(i => Starter.CreateNewContractTester(minersKeyPairs[i])).ToList();
        }

        [Fact]
        public async Task Query_basic_Info()
        {
            
        }
        
        [Fact]
        public async Task Query_Candidate_Info()
        {
        }
        
        [Fact]
        public async Task Query_Tickets_Info()
        {
            await Vote();
            
            // Change the block age 
            _blockAge = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge))).DeserializeToInt64();
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(_blockAge + 180);
            
            //Check duration day 
            var getDurationDays1 = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), _votingRecordList[0], _blockAge + 180)).DeserializeToInt64();
            getDurationDays1.ShouldBe(_lockTimes[0]);
            
            var getDurationDays2 = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), _votingRecordList[2], _blockAge + 180)).DeserializeToInt64();
            getDurationDays2.ShouldBe(_blockAge + 180);
            
            //GetExpireTermNumber
            var expireTermNumber = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetExpireTermNumber),_votingRecordList[0],_blockAge + 180)).DeserializeToInt64();
            expireTermNumber.ShouldBe(_votingRecordList[0].TermNumber + getDurationDays1/ConsensusDPoSConsts.DaysEachTerm);
            
            //QueryObtainedNotExpiredVotes
            var notExpireVotes = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedNotExpiredVotes), _candidateLists[0].PublicKey)).DeserializeToInt64();
            notExpireVotes.ShouldBe(1000L);
           
            //QueryObtainedVotes
            var obtainedVotes = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedVotes), _candidateLists[0].PublicKey)).DeserializeToInt64();
            obtainedVotes.ShouldBe(3000L);   
            
            //GetTicketsInformation
            var candidateTicketsInfo = await _candidateLists[0].GetTicketsInformationAsync();
            candidateTicketsInfo.VotedTickets.ShouldBe(0L);
            candidateTicketsInfo.ObtainedTickets.ShouldBe(3000L);
            candidateTicketsInfo.VotingRecordsCount.ShouldBe(3L);
            candidateTicketsInfo.VoteFromTransactions.Count.ShouldBe(3);
            
            var voterTicketsInfo = await _voterList[0].GetTicketsInformationAsync();
            voterTicketsInfo.VotedTickets.ShouldBe(3000L);
            voterTicketsInfo.ObtainedTickets.ShouldBe(0L);
            voterTicketsInfo.VotingRecordsCount.ShouldBe(3L);
            voterTicketsInfo.VoteToTransactions.Count.ShouldBe(3);
            
            //GetPageableTicketsInfo
            
            //Withdraw all
            var withdrawResult =
                await _voterList[0].ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawAll));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //GetPageableNotWithdrawnTicketsInfo

            
            //GetPageableTicketsHistories

        }

        [Fact]
        public async Task Query_Dividends_Info()
        {
            await Vote();
            
            var previousTermNumber = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetCurrentTermNumber))).DeserializeToInt64();

            // Change term
            await Miners.RunConsensusAsync(1, true);
            
            //Query dividends
            var queryCurrentDividendsForVoters = (await Starter.CallContractMethodAsync(
                Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryCurrentDividendsForVoters))).DeserializeToInt64();
            queryCurrentDividendsForVoters.ShouldBe((long) (DPoSContractConsts.ElfTokenPerBlock * 0.2));

            var queryCurrentDividends = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryCurrentDividends))).DeserializeToInt64();
            queryCurrentDividends.ShouldBe(DPoSContractConsts.ElfTokenPerBlock);
            
            // Get previous term Dividends
            var getTermDividends = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetTermDividends), previousTermNumber)).DeserializeToInt64();
            getTermDividends.ShouldBeGreaterThan(0L);
            
            // Check Dividends
            var termTotalWeights = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetTermTotalWeights), previousTermNumber)).DeserializeToInt64();
            var checkDividends = await Starter.CallContractMethodAsync(
                Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.CheckDividends), Amount, _lockTimes[0], previousTermNumber);
            var dividends = _votingRecordList[0].Weight * getTermDividends / termTotalWeights;
            checkDividends.DeserializeToInt64().ShouldBe(dividends);
            
            // Check Previous Term Dividends
            var votingGains = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.CheckDividends), 10000, 180, previousTermNumber)).DeserializeToInt64();
            var checkPreviousTermDividends = await Starter.CheckDividendsOfPreviousTerm();
            checkPreviousTermDividends.Values[1].ShouldBe(votingGains);

            //Check the next term dividends
            var checkDividendsError = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.CheckDividends), Amount, _lockTimes[0], previousTermNumber + 1)).DeserializeToInt64();
            checkDividendsError.ShouldBe(0L);
            
            await Starter.SetBlockchainAgeAsync(10);
            
            //Get available dividends
            var getAvailableDividends = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetAvailableDividends), _votingRecordList[0])).DeserializeToInt64();
            getAvailableDividends.ShouldBeGreaterThan(0);
            
            //Get all available dividends
            var getAllAvailableDividends =  (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
               nameof(DividendsContract.GetAllAvailableDividends),_voterList[0].PublicKey )).DeserializeToInt64();
            getAllAvailableDividends.ShouldBeGreaterThan(0);
        }
       
        private async Task Vote()
        {
            _lockTimes = new List<int> {90, 180, 730};
            _votingRecordList = new List<VotingRecord>();
            _candidateLists = await Starter.GenerateCandidatesAsync(3);
            _voterList = await Starter.GenerateVotersAsync(3);

            for (int i = 0; i < _voterList.Count; i++)
            {
                await Starter.IssueTokenAsync(_voterList[i].GetCallOwnerAddress(), 100000);
                
                for (int j = 0; j < _candidateLists.Count; j++)
                {
                    
                    var txResult = await _voterList[i].Vote(_candidateLists[i].PublicKey, Amount, _lockTimes[j]);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var votingRecord = await _voterList[i].GetVotingRecord(txResult.TransactionId);
                    _votingRecordList.Add(votingRecord);
                }
            }
            await Miners.RunConsensusAsync(1, true);
        }
    }
}