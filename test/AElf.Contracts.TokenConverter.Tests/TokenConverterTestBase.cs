using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Treasury;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.EconomicSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.TokenConverter;

public class TokenConverterTestBase : AEDPoSExtensionTestBase
{
    public TokenConverterTestBase()
    {
        ContractAddresses = AsyncHelper.RunSync(() => DeploySystemSmartContracts(new List<Hash>
        {
            ProfitSmartContractAddressNameProvider.Name,
            TokenSmartContractAddressNameProvider.Name,
            TokenConverterSmartContractAddressNameProvider.Name,
            TreasurySmartContractAddressNameProvider.Name,
            ParliamentSmartContractAddressNameProvider.Name,
            ConsensusSmartContractAddressNameProvider.Name
        }));
        AsyncHelper.RunSync(InitializeParliamentContractAsync);
        AsyncHelper.RunSync(InitializeTokenAsync);
    }

    protected async Task<long> GetBalanceAsync(string symbol, Address owner)
    {
        var balanceResult = await TokenContractStub.GetBalance.CallAsync(
            new GetBalanceInput
            {
                Owner = owner,
                Symbol = symbol
            });
        return balanceResult.Balance;
    }

    private async Task InitializeTokenAsync()
    {
        await ExecuteProposalForParliamentTransaction(TokenContractAddress, nameof(TokenContractStub.Create),
            new CreateInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000_0000L,
                Issuer = DefaultSender,
                Owner = DefaultSender,
                LockWhiteList = { TokenContractAddress, TokenConverterContractAddress }
            });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = 1000_000L,
            To = DefaultSender,
            Memo = "Set for token converter."
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ELF",
            Amount = 100_0000_0000L,
            To = ManagerAddress,
            Memo = "Set for token converter."
        });
    }

    protected async Task InitializeParliamentContractAsync()
    {
        var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput
        {
            PrivilegedProposer = DefaultSender,
            ProposerAuthorityRequired = true
        });
        CheckResult(initializeResult.TransactionResult);
    }

    protected async Task InitializeTreasuryContractAsync()
    {
        await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
        await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());
    }

    private void CheckResult(TransactionResult result)
    {
        if (!string.IsNullOrEmpty(result.Error)) throw new Exception(result.Error);
    }

    protected async Task ExecuteProposalForParliamentTransaction(Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalHash =
            await CreateAndApproveProposalForParliament(contract, method, input,
                parliamentOrganization);
        await ParliamentContractStub.Release.SendAsync(proposalHash);
    }

    private async Task<Hash> CreateAndApproveProposalForParliament(Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = parliamentOrganization,
            ContractMethodName = method,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contract
        };
        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalHash = createResult.Output;
        await ApproveByParliamentMembers(proposalHash);
        return proposalHash;
    }

    protected async Task<TransactionResult> ExecuteProposalForParliamentTransactionWithException(
        Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalHash =
            await CreateAndApproveProposalForParliament(contract, method, input,
                parliamentOrganization);
        var releaseResult = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalHash);
        return releaseResult.TransactionResult;
    }

    private async Task ApproveByParliamentMembers(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            await tester.Approve.SendAsync(proposalId);
        }
    }

    #region Contract Address

    protected Address TokenContractAddress =>
        ContractAddresses[TokenSmartContractAddressNameProvider.Name];

    protected Address TreasuryContractAddress =>
        ContractAddresses[TreasurySmartContractAddressNameProvider.Name];

    protected Address TokenConverterContractAddress =>
        ContractAddresses[TokenConverterSmartContractAddressNameProvider.Name];

    protected Address ParliamentContractAddress =>
        ContractAddresses[ParliamentSmartContractAddressNameProvider.Name];

    #endregion

    #region Stubs

    internal TokenContractContainer.TokenContractStub TokenContractStub =>
        GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

    internal TokenConverterContractImplContainer.TokenConverterContractImplStub DefaultStub =>
        GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(TokenConverterContractAddress,
            DefaultSenderKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
        GetParliamentContractTester(DefaultSenderKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal TreasuryContractImplContainer.TreasuryContractImplStub TreasuryContractStub =>
        GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
            DefaultSenderKeyPair);

    #endregion

    #region Properties

    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;
    protected ECKeyPair ManagerKeyPair => Accounts[11].KeyPair;
    protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs => Accounts.Take(5).Select(a => a.KeyPair).ToList();

    #endregion
}