using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    // ReSharper disable once InconsistentNaming
    public partial class EconomicTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthStub =>
            GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(
                ContractAddresses[ParliamentAuthSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal ElectionContractContainer.ElectionContractStub ElectionStub =>
            GetTester<ElectionContractContainer.ElectionContractStub>(
                ContractAddresses[ElectionSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal EconomicContractContainer.EconomicContractStub EconomicStub =>
            GetTester<EconomicContractContainer.EconomicContractStub>(
                ContractAddresses[EconomicSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryStub =>
            GetTester<TreasuryContractContainer.TreasuryContractStub>(
                ContractAddresses[TreasurySmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        internal ProfitContractContainer.ProfitContractStub ProfitStub =>
            GetTester<ProfitContractContainer.ProfitContractStub>(
                ContractAddresses[ProfitSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        public EconomicTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                VoteSmartContractAddressNameProvider.Name,
                ProfitSmartContractAddressNameProvider.Name,
                ParliamentAuthSmartContractAddressNameProvider.Name,
                ElectionSmartContractAddressNameProvider.Name,
                TreasurySmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                EconomicSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitialEconomicSystem);
        }

        private async Task InitialEconomicSystem()
        {
            // Profit distribution schemes related to Treasury must be created before initialization of Economic System.
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                TreasuryStub.InitialTreasuryContract.GetTransaction(new Empty()),
                TreasuryStub.InitialMiningRewardProfitItem.GetTransaction(new Empty())
            });
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                ElectionStub.InitialElectionContract.GetTransaction(new InitialElectionContractInput
                {
                    MinerList = {MissionedECKeyPairs.InitialKeyPairs.Select(p => p.PublicKey.ToHex())},
                    MinerIncreaseInterval = AEDPoSExtensionConstants.MinerIncreaseInterval,
                    TimeEachTerm = AEDPoSExtensionConstants.TimeEachTerm,
                    MinimumLockTime = EconomicTestConstants.MinimumLockTime,
                    MaximumLockTime = EconomicTestConstants.MaximumLockTime
                }),
                ParliamentAuthStub.Initialize.GetTransaction(new InitializeInput
                {
                    GenesisOwnerReleaseThreshold = 6666
                }),
                EconomicStub.InitialEconomicSystem.GetTransaction(new InitialEconomicSystemInput
                {
                    IsNativeTokenBurnable = true,
                    MiningRewardTotalAmount = 2_000_000_000_00000000,
                    NativeTokenDecimals = 8,
                    NativeTokenSymbol = EconomicTestConstants.TokenSymbol,
                    NativeTokenTotalSupply = 10_000_000_000_00000000,
                    NativeTokenName = "Native Token"
                })
            });
            await BlockMiningService.MineBlockAsync(GetIssueTransactions());
        }

        private List<Transaction> GetIssueTransactions()
        {
            var issueTransactions = new List<Transaction>();
            foreach (var coreDataCenterKeyPair in MissionedECKeyPairs.CoreDataCenterKeyPairs
                .Concat(MissionedECKeyPairs.ValidationDataCenterKeyPairs).Concat(MissionedECKeyPairs.CitizenKeyPairs))
            {
                issueTransactions.Add(EconomicStub.IssueNativeToken.GetTransaction(new IssueNativeTokenInput
                {
                    To = Address.FromPublicKey(coreDataCenterKeyPair.PublicKey),
                    Amount = 8_000_000_000_00000000 / 100,
                }));
            }

            return issueTransactions;
        }
    }
}