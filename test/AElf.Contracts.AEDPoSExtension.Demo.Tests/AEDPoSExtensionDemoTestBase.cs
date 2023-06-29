using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.RandomNumberProvider;
using AElf.Contracts.Treasury;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

// ReSharper disable once InconsistentNaming
public class AEDPoSExtensionDemoTestBase : AEDPoSExtensionTestBase
{
    internal readonly List<ParliamentContractImplContainer.ParliamentContractImplStub> ParliamentStubs = new();

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub =>
        GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(
            ContractZeroAddress, Accounts[0].KeyPair);

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
        GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
            ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
            Accounts[0].KeyPair);

    internal ElectionContractContainer.ElectionContractStub ElectionStub =>
        GetTester<ElectionContractContainer.ElectionContractStub>(
            ContractAddresses[ElectionSmartContractAddressNameProvider.Name],
            Accounts[10].KeyPair);

    internal EconomicContractContainer.EconomicContractStub EconomicStub =>
        GetTester<EconomicContractContainer.EconomicContractStub>(
            ContractAddresses[EconomicSmartContractAddressNameProvider.Name],
            Accounts[0].KeyPair);

    internal TreasuryContractContainer.TreasuryContractStub TreasuryStub =>
        GetTester<TreasuryContractContainer.TreasuryContractStub>(
            ContractAddresses[TreasurySmartContractAddressNameProvider.Name],
            Accounts[0].KeyPair);

    internal TokenContractImplContainer.TokenContractImplStub TokenStub =>
        GetTester<TokenContractImplContainer.TokenContractImplStub>(
            ContractAddresses[TokenSmartContractAddressNameProvider.Name],
            Accounts[0].KeyPair);

    internal AssociationContractImplContainer.AssociationContractImplStub AssociationStub =>
        GetTester<AssociationContractImplContainer.AssociationContractImplStub>(
            ContractAddresses[AssociationSmartContractAddressNameProvider.Name],
            Accounts[0].KeyPair);

    internal void InitialContracts()
    {
        ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
        {
            // You can deploy more system contracts by adding system contract name to current list.
            TokenSmartContractAddressNameProvider.Name,
            ParliamentSmartContractAddressNameProvider.Name,
            ElectionSmartContractAddressNameProvider.Name,
            AssociationSmartContractAddressNameProvider.Name,
            ReferendumSmartContractAddressNameProvider.Name
        }));
    }

    internal void InitialAcs3Stubs()
    {
        foreach (var initialKeyPair in MissionedECKeyPairs.InitialKeyPairs)
        {
            var parliamentStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                ContractAddresses[ParliamentSmartContractAddressNameProvider.Name], initialKeyPair);

            ParliamentStubs.Add(parliamentStub);
        }
    }

    internal async Task<RandomNumberProviderContractContainer.RandomNumberProviderContractStub>
        DeployRandomNumberProviderContract()
    {
        var address = (await BasicContractZeroStub.DeploySmartContract.SendAsync(new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory,
            Code = ByteString.CopyFrom(ContractsDeployer.GetContractCodes<AEDPoSExtensionDemoModule>()
                .Single(kv => kv.Key.EndsWith("RandomNumberProvider"))
                .Value)
        })).Output;

        return GetTester<RandomNumberProviderContractContainer.RandomNumberProviderContractStub>(address,
            Accounts[0].KeyPair);
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
        foreach (var stub in ParliamentStubs) approvals.Add(stub.Approve.GetTransaction(proposalId));

        await BlockMiningService.MineBlockAsync(approvals);

        await ParliamentStubs.First().Release.SendAsync(proposalId);
    }
    
    internal async Task<TransactionResult> ParliamentReachAnAgreementWithExceptionAsync(CreateProposalInput createProposalInput)
    {
        var createProposalTx = ParliamentStubs.First().CreateProposal.GetTransaction(createProposalInput);
        await BlockMiningService.MineBlockAsync(new List<Transaction>
        {
            createProposalTx
        });
        var proposalId = new Hash();
        proposalId.MergeFrom(TransactionTraceProvider.GetTransactionTrace(createProposalTx.GetHash()).ReturnValue);
        var approvals = new List<Transaction>();
        foreach (var stub in ParliamentStubs) approvals.Add(stub.Approve.GetTransaction(proposalId));

        await BlockMiningService.MineBlockAsync(approvals);

        return (await ParliamentStubs.First().Release.SendWithExceptionAsync(proposalId)).TransactionResult;
    }

    internal void SetToSideChain()
    {
        var chainTypeProvider = GetRequiredService<IChainTypeProvider>();
        chainTypeProvider.IsSideChain = true;
    }
}