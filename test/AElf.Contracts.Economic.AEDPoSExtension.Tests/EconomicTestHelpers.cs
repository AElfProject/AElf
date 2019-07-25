using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    /// <summary>
    /// Some view methods.
    /// </summary>
    public partial class EconomicTestBase
    {
        protected async Task<long> GetBalanceAsync(Address owner)
        {
            return (await TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = owner,
                Symbol = EconomicTestConstants.TokenSymbol
            })).Balance;
        }
        
        internal async Task<DistributedProfitsInfo> GetDistributedInformationAsync(Hash schemeId, long period)
        {
            return await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
            {
                SchemeId = schemeId,
                Period = period
            });
        }

        internal async Task<Dictionary<SchemeType, Scheme>> GetTreasurySchemesAsync()
        {
            var treasurySchemeId = await TreasuryStub.GetTreasurySchemeId.CallAsync(new Empty());
            var schemes = new Dictionary<SchemeType, Scheme>();
            var treasuryScheme = await ProfitStub.GetScheme.CallAsync(treasurySchemeId);
            schemes.Add(SchemeType.Treasury, treasuryScheme);
            var minerRewardScheme = await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[0].SchemeId);
            schemes.Add(SchemeType.MinerReward, minerRewardScheme);
            schemes.Add(SchemeType.BackupSubsidy,
                await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[1].SchemeId));
            schemes.Add(SchemeType.CitizenWelfare,
                await ProfitStub.GetScheme.CallAsync(treasuryScheme.SubSchemes[2].SchemeId));
            schemes.Add(SchemeType.MinerBasicReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[0].SchemeId));
            schemes.Add(SchemeType.VotesWeightReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[1].SchemeId));
            schemes.Add(SchemeType.ReElectionReward,
                await ProfitStub.GetScheme.CallAsync(minerRewardScheme.SubSchemes[2].SchemeId));
            return schemes;
        }

        internal async Task ClaimProfits(IEnumerable<ECKeyPair> keyPairs, Hash schemeId)
        {
            var stubs = ConvertKeyPairsToProfitStubs(keyPairs);
            await BlockMiningService.MineBlockAsync(stubs.Select(s =>
                s.ClaimProfits.GetTransaction(new ClaimProfitsInput
                {
                    SchemeId = schemeId,
                    Symbol = EconomicTestConstants.TokenSymbol
                })).ToList());
        }

        internal async Task CheckBalancesAsync(IEnumerable<ECKeyPair> keyPairs, long shouldBe,
            Dictionary<ECKeyPair, long> balancesBefore = null)
        {
            balancesBefore = balancesBefore ?? new Dictionary<ECKeyPair, long>();
            foreach (var keyPair in keyPairs)
            {
                var amount = await GetBalanceAsync(Address.FromPublicKey(keyPair.PublicKey));
                amount.ShouldBe(shouldBe + balancesBefore[keyPair]);
            }
        }

        internal enum SchemeType
        {
            Treasury,

            MinerReward,
            BackupSubsidy,
            CitizenWelfare,

            MinerBasicReward,
            VotesWeightReward,
            ReElectionReward
        }

        internal List<AEDPoSContractImplContainer.AEDPoSContractImplStub> ConvertKeyPairsToConsensusStubs(
            IEnumerable<ECKeyPair> keyPairs)
        {
            return keyPairs.Select(p =>
                GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                    ContractAddresses[ConsensusSmartContractAddressNameProvider.Name], p)).ToList();
        }
        
        internal List<ProfitContractContainer.ProfitContractStub> ConvertKeyPairsToProfitStubs(
            IEnumerable<ECKeyPair> keyPairs)
        {
            return keyPairs.Select(p =>
                GetTester<ProfitContractContainer.ProfitContractStub>(
                    ContractAddresses[ProfitSmartContractAddressNameProvider.Name], p)).ToList();
        }
        
        internal List<ElectionContractContainer.ElectionContractStub> ConvertKeyPairsToElectionStubs(
            IEnumerable<ECKeyPair> keyPairs)
        {
            return keyPairs.Select(p =>
                GetTester<ElectionContractContainer.ElectionContractStub>(
                    ContractAddresses[ElectionSmartContractAddressNameProvider.Name], p)).ToList();
        }

        internal async Task<List<Transaction>> GetVoteTransactionsAsync(int timesOfMinimumLockDays,
            long amount, string candidatePubkey, int votersCount = 0)
        {
            if (votersCount > AEDPoSExtensionConstants.CitizenKeyPairsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(votersCount), "Didn't prepare this amount of citizens.");
            }

            votersCount = votersCount == 0 ? AEDPoSExtensionConstants.CitizenKeyPairsCount : votersCount;

            var voteTransaction = new List<Transaction>();

            var blockTime = TestDataProvider.GetBlockTime();

            var electionStubs = ConvertKeyPairsToElectionStubs(MissionedECKeyPairs.CitizenKeyPairs.Take(votersCount));

            foreach (var electionStub in electionStubs)
            {
                voteTransaction.Add(electionStub.Vote.GetTransaction(new VoteMinerInput
                {
                    CandidatePubkey = candidatePubkey,
                    Amount = amount,
                    EndTimestamp =
                        blockTime.AddSeconds(timesOfMinimumLockDays * EconomicTestConstants.MinimumLockTime)
                }));
            }

            return voteTransaction;
        }

        internal async Task<long> MineBlocksToNextTermAsync(long currentTermNumber = 0)
        {
            currentTermNumber = currentTermNumber == 0
                ? (await ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value
                : currentTermNumber;
            var targetTermNumber = currentTermNumber + 1;
            var actualTermNumber = 0L;
            while (actualTermNumber != targetTermNumber)
            {
                await BlockMiningService.MineBlockAsync();
                actualTermNumber = (await ConsensusStub.GetCurrentTermNumber.CallAsync(new Empty())).Value;
            }

            return (await ConsensusStub.GetMinedBlocksOfPreviousTerm.CallAsync(new Empty())).Value;
        }
    }
}