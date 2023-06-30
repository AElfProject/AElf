using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Contracts.CrossChain.Tests;

public class CrossChainContractTestBase<T> : ContractTestBase<T> where T : AbpModule
{
    public CrossChainContractTestBase()
    {
        AsyncHelper.RunSync(InitializeTokenAsync);
    }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

    protected ECKeyPair AnotherKeyPair => Accounts.Last().KeyPair;
    protected Address AnotherSender => Accounts.Last().Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected Address DefaultSender => Address.FromPublicKey(DefaultKeyPair.PublicKey);

    protected Address AnotherSenderAddress => Address.FromPublicKey(AnotherKeyPair.PublicKey);

    protected IBlockTimeProvider BlockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

    #region Token

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub =>
        GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);

    #endregion

    internal CrossChainContractImplContainer.CrossChainContractImplStub CrossChainContractStub =>
        GetCrossChainContractStub(DefaultKeyPair);

    protected List<string> ResourceTokenSymbolList =>
        GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>()
            .Value.ContextVariables["SymbolListToPayRental"].Split(",").ToList();

    internal CrossChainContractImplContainer.CrossChainContractImplStub GetCrossChainContractStub(
        ECKeyPair keyPair)
    {
        return GetTester<CrossChainContractImplContainer.CrossChainContractImplStub>(
            CrossChainContractAddress,
            keyPair);
    }

    internal TokenContractImplContainer.TokenContractImplStub GetTokenContractStub(ECKeyPair keyPair)
    {
        return GetTester<TokenContractImplContainer.TokenContractImplStub>(
            TokenContractAddress,
            keyPair);
    }

    protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
        int parentChainId = 0, bool withException = false)
    {
        var tx = CrossChainContractStub.Initialize.GetTransaction(new InitializeInput
        {
            ParentChainId = parentChainId,
            CreationHeightOnParentChain = parentChainHeightOfCreation
        });

        var blockExecutedSet = await MineAsync(new List<Transaction>
        {
            tx
        });
        (blockExecutedSet.TransactionResultMap[tx.GetHash()].Status == TransactionResultStatus.Failed).ShouldBe(
            withException);
    }

    internal async Task<int> InitAndCreateSideChainAsync(long parentChainHeightOfCreation = 0,
        int parentChainId = 0, long lockedTokenAmount = 10, long indexingFee = 1, ECKeyPair keyPair = null,
        bool withException = false, bool isPrivilegeReserved = false)
    {
        await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId, withException);
        await ApproveBalanceAsync(lockedTokenAmount, keyPair);
        var proposalId =
            await CreateSideChainProposalAsync(indexingFee, lockedTokenAmount, keyPair, null, isPrivilegeReserved);
        await ApproveWithMinersAsync(proposalId);

        var crossChainContractStub = keyPair == null ? CrossChainContractStub : GetCrossChainContractStub(keyPair);
        var releaseTx =
            await crossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                { ProposalId = proposalId });
        var sideChainCreatedEvent = SideChainCreatedEvent.Parser
            .ParseFrom(releaseTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed);
        var chainId = sideChainCreatedEvent.ChainId;

        return chainId;
    }

    internal async Task<TransactionResult> CreateSideChainAsync(bool initCrossChainContract,
        long parentChainHeightOfCreation,
        int parentChainId, long lockedTokenAmount, long indexingFee, bool isPrivilegeReserved)
    {
        if (initCrossChainContract)
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
        await ApproveBalanceAsync(lockedTokenAmount);
        var proposalId =
            await CreateSideChainProposalAsync(indexingFee, lockedTokenAmount, null, null, isPrivilegeReserved);
        await ApproveWithMinersAsync(proposalId);
        var releaseTx =
            await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(new ReleaseSideChainCreationInput
                { ProposalId = proposalId });

        return releaseTx.TransactionResult;
    }

    internal async Task<int> CreateSideChainByDefaultSenderAsync(bool initCrossChainContract,
        long parentChainHeightOfCreation = 0,
        int parentChainId = 0, long lockedTokenAmount = 10, long indexingFee = 1, bool isPrivilegeReserved = false)
    {
        var releaseTxResult =
            await CreateSideChainAsync(initCrossChainContract, parentChainHeightOfCreation, parentChainId,
                lockedTokenAmount,
                indexingFee, isPrivilegeReserved);
        var sideChainCreatedEvent = SideChainCreatedEvent.Parser.ParseFrom(releaseTxResult.Logs
            .First(l => l.Name.Contains(nameof(SideChainCreatedEvent))).NonIndexed);

        var sideChainId = sideChainCreatedEvent.ChainId;
        return sideChainId;
    }

    private async Task InitializeParliamentContractAsync()
    {
        var initializeResult = await ParliamentContractStub.Initialize.SendAsync(
            new Parliament.InitializeInput
            {
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = false
            });
        CheckResult(initializeResult.TransactionResult);
    }

    private async Task InitializeTokenAsync()
    {
        const string symbol = "ELF";
        const long totalSupply = 100_000_000;
        
        var parliamentOrganizationAddress =
            (await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty()));
        var approveProposalId = await CreateParliamentProposalAsync(nameof(TokenContractStub.Create),
            parliamentOrganizationAddress, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender
            }, TokenContractAddress);
        await ApproveWithMinersAsync(approveProposalId);
        await ParliamentContractStub.Release.SendAsync(approveProposalId);

        await MineAsync(new List<Transaction>
        {
            TokenContractStub.Issue.GetTransaction(new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user."
            })
        });
    }

    protected async Task ApproveBalanceAsync(long amount, ECKeyPair keyPair = null)
    {
        var tokenContractStub = keyPair == null ? TokenContractStub : GetTokenContractStub(keyPair);
        await MineAsync(new List<Transaction>
        {
            tokenContractStub.Approve.GetTransaction(new ApproveInput
            {
                Spender = CrossChainContractAddress,
                Symbol = "ELF",
                Amount = amount
            }),
            tokenContractStub.GetAllowance.GetTransaction(new GetAllowanceInput
            {
                Symbol = "ELF",
                Owner = DefaultSender,
                Spender = CrossChainContractAddress
            })
        });
    }

    internal async Task<GetAllowanceOutput> ApproveAndTransferOrganizationBalanceAsync(Address organizationAddress,
        long amount)
    {
        var approveInput = new ApproveInput
        {
            Spender = CrossChainContractAddress,
            Symbol = "ELF",
            Amount = amount
        };
        var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.Approve),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = approveInput.ToByteString(),
            ToAddress = TokenContractAddress,
            OrganizationAddress = organizationAddress
        })).Output;
        await ApproveWithMinersAsync(proposal);
        await ReleaseProposalAsync(proposal);

        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Symbol = "ELF",
            Amount = amount,
            To = organizationAddress
        });

        var allowance = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Symbol = "ELF",
            Owner = organizationAddress,
            Spender = CrossChainContractAddress
        });

        return allowance;
    }

    internal async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
        ECKeyPair keyPair = null,
        Dictionary<string, int> resourceAmount = null, bool isPrivilegeReserved = false)
    {
        var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount,
            resourceAmount ?? GetValidResourceAmount(), new[]
            {
                new SideChainTokenInitialIssue
                {
                    Address = DefaultSender,
                    Amount = 100
                }
            }, isPrivilegeReserved);
        var crossChainContractStub = keyPair == null ? CrossChainContractStub : GetCrossChainContractStub(keyPair);
        var requestSideChainCreation =
            await crossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);

        var proposalId = ProposalCreated.Parser.ParseFrom(requestSideChainCreation.TransactionResult.Logs
            .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
        return proposalId;
    }

    internal async Task<Hash> CreateParliamentProposalAsync(string method, Address organizationAddress,
        IMessage input, Address toAddress = null)
    {
        var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ToAddress = toAddress ?? CrossChainContractAddress,
            ContractMethodName = method,
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = organizationAddress,
            Params = input.ToByteString()
        })).Output;
        return proposal;
    }

    internal async Task<Hash> CreateAssociationProposalAsync(string method, Address organizationAddress,
        Address toAddress,
        IMessage input, AssociationContractImplContainer.AssociationContractImplStub authorizationContractStub = null)
    {
        var proposalId = (await (authorizationContractStub ?? AssociationContractStub).CreateProposal.SendAsync(
            new CreateProposalInput
            {
                ToAddress = toAddress,
                ContractMethodName = method,
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress,
                Params = input.ToByteString()
            })).Output;
        return proposalId;
    }

    protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
    {
        var transaction = await ParliamentContractStub.Release.SendAsync(proposalId);
        return transaction.TransactionResult;
    }

    protected async Task<TransactionResult> ReleaseProposalWithExceptionAsync(Hash proposalId)
    {
        var transaction = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        return transaction.TransactionResult;
    }

    internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
        Dictionary<string, int> resourceAmount, SideChainTokenInitialIssue[] sideChainTokenInitialIssueList,
        bool isPrivilegePreserved = false)
    {
        var res = new SideChainCreationRequest
        {
            IndexingPrice = indexingPrice,
            LockedTokenAmount = lockedTokenAmount,
            SideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenDecimals = 2,
                SideChainTokenTotalSupply = 1_000_000_000,
                SideChainTokenSymbol = "TE",
                SideChainTokenName = "TEST"
            },
            SideChainTokenInitialIssueList = { sideChainTokenInitialIssueList },
            InitialResourceAmount = { resourceAmount },
            IsPrivilegePreserved = isPrivilegePreserved
        };
        return res;
    }

    internal Dictionary<string, int> GetValidResourceAmount()
    {
        return ResourceTokenSymbolList.ToDictionary(resource => resource, resource => 1);
    }

    protected async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            CheckResult(approveResult.TransactionResult);
        }
    }

    protected async Task<long> GetBalance(Address address, string symbol = "ELF")
    {
        return (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = address,
            Symbol = "ELF"
        })).Balance;
    }

    internal void AssertChainIndexingProposalStatus(
        PendingChainIndexingProposalStatus pendingChainIndexingProposalStatus,
        Address expectedProposer, Hash expectedProposalId, CrossChainBlockData expectedCrossChainData,
        bool toBeReleased)
    {
        pendingChainIndexingProposalStatus.ProposalId.ShouldBe(expectedProposalId);
        pendingChainIndexingProposalStatus.Proposer.ShouldBe(expectedProposer);
        pendingChainIndexingProposalStatus.ProposedCrossChainBlockData.ShouldBe(expectedCrossChainData);
        pendingChainIndexingProposalStatus.ToBeReleased.ShouldBe(toBeReleased);
    }

    internal async Task<long> DoIndexAsync(CrossChainBlockData crossChainBlockData, int[] chainIdList)
    {
        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalIdList = txRes.TransactionResult.Logs.Where(l => l.Name.Contains(nameof(ProposalCreated)))
            .Select(e => ProposalCreated.Parser.ParseFrom(e.NonIndexed).ProposalId);
        foreach (var proposalId in proposalIdList) await ApproveWithMinersAsync(proposalId);

        var txResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { chainIdList }
            });
        return txResult.TransactionResult.BlockNumber;
    }

    internal async Task<Hash> DisposeSideChainProposalAsync(Int32Value chainId)
    {
        var disposalInput = chainId;
        var organizationAddress =
            (await CrossChainContractStub.GetSideChainLifetimeController.CallAsync(new Empty())).OwnerAddress;
        var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(CrossChainContractStub.DisposeSideChain),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = disposalInput.ToByteString(),
            ToAddress = CrossChainContractAddress,
            OrganizationAddress = organizationAddress
        })).Output;
        return proposal;
    }

    internal async Task<long> GetSideChainBalanceAsync(int chainId)
    {
        return (await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value { Value = chainId })).Value;
    }

    internal async Task<SideChainStatus> GetSideChainStatusAsync(int chainId)
    {
        return (await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value { Value = chainId })).Status;
    }

    private void CheckResult(TransactionResult result)
    {
        if (!string.IsNullOrEmpty(result.Error)) throw new Exception(result.Error);
    }

    internal ParentChainBlockData CreateParentChainBlockData(long height, int sideChainId, Hash txMerkleTreeRoot)
    {
        return new ParentChainBlockData
        {
            ChainId = sideChainId,
            Height = height,
            TransactionStatusMerkleTreeRoot = txMerkleTreeRoot
        };
    }

    #region Contract Address

    #endregion

    #region Paliament

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
        GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            DefaultKeyPair);

    internal AssociationContractImplContainer.AssociationContractImplStub AssociationContractStub =>
        GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress,
            DefaultKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal AssociationContractImplContainer.AssociationContractImplStub GetAssociationContractStub(ECKeyPair keyPair)
    {
        return GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress,
            keyPair);
    }

    #endregion
}