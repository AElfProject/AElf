using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.ContractTestKit;
using AElf.Contracts.Treasury;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    // ReSharper disable once InconsistentNaming
    public partial class EconomicTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
            GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                ContractAddresses[ParliamentSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal ElectionContractContainer.ElectionContractStub ElectionStub =>
            GetTester<ElectionContractContainer.ElectionContractStub>(
                ContractAddresses[ElectionSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal EconomicContractContainer.EconomicContractStub EconomicStub =>
            GetTester<EconomicContractContainer.EconomicContractStub>(
                ContractAddresses[EconomicSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryStub =>
            GetTester<TreasuryContractContainer.TreasuryContractStub>(
                ContractAddresses[TreasurySmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);

        internal ProfitContractContainer.ProfitContractStub ProfitStub =>
            GetTester<ProfitContractContainer.ProfitContractStub>(
                ContractAddresses[ProfitSmartContractAddressNameProvider.Name],
                Accounts[0].KeyPair);
        
        internal readonly List<ParliamentContractImplContainer.ParliamentContractImplStub> ParliamentStubs =
            new List<ParliamentContractImplContainer.ParliamentContractImplStub>();

        public EconomicTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
                VoteSmartContractAddressNameProvider.Name,
                ProfitSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                ElectionSmartContractAddressNameProvider.Name,
                TreasurySmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                EconomicSmartContractAddressNameProvider.Name,
                TokenHolderSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name
            }));

            AsyncHelper.RunSync(InitialEconomicSystem);
        }

        private async Task InitialEconomicSystem()
        {
            // Profit distribution schemes related to Treasury must be created before initialization of Economic System.
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                ParliamentContractStub.Initialize.GetTransaction(new InitializeInput()),
                TreasuryStub.InitialTreasuryContract.GetTransaction(new Empty()),
                TreasuryStub.InitialMiningRewardProfitItem.GetTransaction(new Empty())
            });
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                ElectionStub.InitialElectionContract.GetTransaction(new InitialElectionContractInput
                {
                    MinerList = {MissionedECKeyPairs.InitialKeyPairs.Select(p => p.PublicKey.ToHex())},
                    MinerIncreaseInterval = AEDPoSExtensionConstants.MinerIncreaseInterval,
                    TimeEachTerm = AEDPoSExtensionConstants.PeriodSeconds,
                    MinimumLockTime = EconomicTestConstants.MinimumLockTime,
                    MaximumLockTime = EconomicTestConstants.MaximumLockTime
                }),
                EconomicStub.InitialEconomicSystem.GetTransaction(new InitialEconomicSystemInput
                {
                    IsNativeTokenBurnable = true,
                    MiningRewardTotalAmount = 1_200_000_000_00000000,
                    NativeTokenDecimals = 8,
                    NativeTokenSymbol = EconomicTestConstants.TokenSymbol,
                    NativeTokenTotalSupply = 10_000_000_000_00000000,
                    NativeTokenName = "Native Token",
                    TransactionSizeFeeUnitPrice = 1000
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
                    Amount = 8_800_000_000_00000000 / Accounts.Count,
                }));
            }

            return issueTransactions;
        }
        
        internal async Task ParliamentReachAnAgreementAsync(CreateProposalInput createProposalInput)
        {
            var createProposalTx = ParliamentStubs.First().CreateProposal.GetTransaction(createProposalInput);
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                createProposalTx
            });
            var proposalId = new Hash();
            proposalId.MergeFrom(TransactionTraceProvider.GetTransactionTrace(createProposalTx.GetHash()).ReturnValue);
            var approvals = new List<Transaction>();
            foreach (var stub in ParliamentStubs)
            {
                approvals.Add(stub.Approve.GetTransaction(proposalId));
            }
            
            await BlockMiningService.MineBlockAsync(approvals);

            await ParliamentStubs.First().Release.SendAsync(proposalId);
        }
        
        internal void UpdateParliamentStubs(IEnumerable<ECKeyPair> keyPairs)
        {
            ParliamentStubs.Clear();
            foreach (var initialKeyPair in keyPairs)
            {
                ParliamentStubs.Add(GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                    ContractAddresses[ParliamentSmartContractAddressNameProvider.Name], initialKeyPair));
            }
        }
    }
}