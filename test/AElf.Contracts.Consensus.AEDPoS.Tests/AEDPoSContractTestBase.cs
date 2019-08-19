using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class AEDPoSContractTestBase : EconomicContractsTestBase
    {
        private new void DeployAllContracts()
        {
            _ = TokenContractAddress;
            _ = VoteContractAddress;
            _ = ProfitContractAddress;
            _ = EconomicContractAddress;
            _ = ElectionContractAddress;
            _ = TreasuryContractAddress;
            _ = TransactionFeeChargingContractAddress;
            _ = ParliamentAuthContractAddress;
            _ = TokenConverterContractAddress;
            _ = ConsensusContractAddress;
        }
        protected void InitializeContracts()
        {
            DeployAllContracts();

            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
        }

        protected IAElfAsymmetricCipherKeyPairProvider KeyPairProvider =>
            Application.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();

        protected ITriggerInformationProvider TriggerInformationProvider =>
            Application.ServiceProvider.GetRequiredService<ITriggerInformationProvider>();
        protected Timestamp BlockchainStartTimestamp => new Timestamp {Seconds = 0};

        internal TokenContractContainer.TokenContractStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

        internal VoteContractContainer.VoteContractStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal ElectionContractContainer.ElectionContractStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);
        
        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
        }

        internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }

        internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }
        
        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
        }
        
        protected async Task InitializeCandidates(int take = EconomicContractsTestConstants.ValidateDataCenterCount)
        {
            foreach (var candidatesKeyPair in ValidationDataCenterKeyPairs.Take(take))
            {
                var electionTester = GetElectionContractTester(candidatesKeyPair);
                var announceResult = await electionTester.AnnounceElection.SendAsync(new Empty());
                announceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                //query candidates
                var candidates = await electionTester.GetCandidates.CallAsync(new Empty());
                candidates.Value.Select(o=>o.ToByteArray().ToHex()).Contains(candidatesKeyPair.PublicKey.ToHex()).ShouldBeTrue();
            }
        }
        
        protected async Task BootMinerChangeRoundAsync(long nextRoundNumber = 2)
        {
            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                .AddMilliseconds(
                    ((long) currentRound.TotalMilliseconds(AEDPoSContractTestConstants.MiningInterval)).Mul(
                        nextRoundNumber.Sub(1)));
            currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                out var nextRound);
            await AEDPoSContractStub.NextRound.SendAsync(nextRound);
        }
    }
}