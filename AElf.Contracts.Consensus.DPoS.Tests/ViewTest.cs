using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividends;
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
        
        private List<TransactionResult>_txResultList ;
        private List<VotingRecord> _votingRecordList ;
        private List<ContractTester<DPoSContractTestAElfModule>> _votersList;
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
            await Vote();
            
        }
        
        [Fact]
        public async Task Query_Candidate_Info()
        {
            await Vote_From_Many_Voters();
        
            // Change the block age 
            _blockAge = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge))).DeserializeToInt64();
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(_blockAge + 365);
            

        }
        
        [Fact]
        public async Task Query_Tickets_Info()
        {
            await Vote();
            
            // Change the block age 
            _blockAge = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.GetBlockchainAge))).DeserializeToInt64();
            await Miners.ChangeTermAsync(MiningInterval);
            await Starter.SetBlockchainAgeAsync(_blockAge + 365);
            
            //Withdraw all
            var withdrawResult =
                await _votersList[0].ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.WithdrawAll));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check duration day 
            var getDurationDays1 = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), _votingRecordList[0], _blockAge + 365)).DeserializeToInt64();
            getDurationDays1.ShouldBe(_lockTimes[0]);
            
            var getDurationDays2 = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetDurationDays), _votingRecordList[3], _blockAge + 365)).DeserializeToInt64();
            getDurationDays2.ShouldBe(_blockAge + 365);
            
            //GetExpireTermNumber
            var expireTermNumber = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetExpireTermNumber),_votingRecordList[0],_blockAge + 365)).DeserializeToInt64();
            expireTermNumber.ShouldBe(_votingRecordList[0].TermNumber + getDurationDays1/ConsensusDPoSConsts.DaysEachTerm);
            
            //QueryObtainedNotExpiredVotes
            var notExpireVotes = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedNotExpiredVotes), _candidateLists[0].PublicKey)).DeserializeToInt64();
            notExpireVotes.ShouldBe(2000);
            
            //QueryObtainedVotes
            var obtainedVotes = (await Starter.CallContractMethodAsync(Starter.GetConsensusContractAddress(),
                nameof(ConsensusContract.QueryObtainedVotes), _candidateLists[0].PublicKey)).DeserializeToInt64();
            obtainedVotes.ShouldBe(5000);
            
            //GetTicketsInformation
            var ticketsInfo = await _votersList[0].GetTicketsInformationAsync();
            ticketsInfo.VotedTickets.ShouldBe(Amount);
            ticketsInfo.ObtainedTickets.ShouldBe(obtainedVotes);
            ticketsInfo.VotingRecordsCount.ShouldBe(5L);
            ticketsInfo.PublicKey.ShouldBe(_votersList[0].PublicKey);
            
            //GetPageableTicketsInfo
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
            await Miners.RunConsensusAsync(3, true);
            //await Miners.ChangeRoundAsync();
            
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
            checkDividends.DeserializeToInt64().ShouldBe(_votingRecordList[0].Weight * getTermDividends / termTotalWeights);

            // Change Term & add block age
            await Miners.RunConsensusAsync(1, true);
            
            var checkPreviousTermDividends = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
              nameof(DividendsContract.CheckDividendsOfPreviousTerm))).DeserializeToInt64();
            checkPreviousTermDividends.ShouldBeGreaterThan(0L);
            
            var checkDividendsError = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.CheckDividends), Amount, _lockTimes[0], previousTermNumber + 1)).DeserializeToInt64();
            checkDividendsError.ShouldBe(0L);
            
            await Starter.SetBlockchainAgeAsync(10);
            //Get available dividends
            var getAvailableDividends = (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
                nameof(DividendsContract.GetAvailableDividends), _votingRecordList[0])).DeserializeToInt64();
            getAvailableDividends.ShouldBeGreaterThan(0);
            
            //GetAllAvailableDividends
            var getAllAvailableDividends =  (await Starter.CallContractMethodAsync(Starter.GetDividendsContractAddress(),
               nameof(DividendsContract.GetAllAvailableDividends),_candidateLists[0].PublicKey )).DeserializeToInt64();
            getAllAvailableDividends.ShouldBeGreaterThan(0);
        }
       
        private async Task Vote()
        {
            _lockTimes = new List<int> {90, 180, 365, 730, 1095};
            _txResultList = new List<TransactionResult>();
            _votingRecordList = new List<VotingRecord>();
            _candidateLists = await Starter.GenerateCandidatesAsync(5);
            _votersList = await Starter.GenerateVotersAsync(5);

            for (int i = 0; i < _votersList.Count; i++)
            {
                await Starter.IssueTokenAsync(_votersList[i].GetCallOwnerAddress(), 100000);
                
                for (int j = 0; j < _candidateLists.Count; j++)
                {
                    var txResult = await _votersList[i].Vote(_candidateLists[j].PublicKey, Amount, _lockTimes[j]);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    _txResultList.Add(txResult);

                    var votingRecord = await _votersList[i].GetVotingRecord(_txResultList[j].TransactionId);
                    _votingRecordList.Add(votingRecord);
                }
            }
            await Miners.RunConsensusAsync(1, true);
        }
    }
}