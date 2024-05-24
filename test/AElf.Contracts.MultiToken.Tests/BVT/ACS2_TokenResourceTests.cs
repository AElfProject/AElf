using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    [Fact]
    public async Task ACS2_GetResourceInfo_Transfer_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.Transfer),
            new TransferInput
            {
                Amount = 100,
                Symbol = "ELF",
                To = Accounts[1].Address,
                Memo = "Test get resource"
            });

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeFalse();
        result.WritePaths.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ACS2_GetResourceInfo_TransferFrom_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.TransferFrom),
            new TransferFromInput
            {
                Amount = 100,
                Symbol = "ELF",
                From = Accounts[1].Address,
                To = Accounts[2].Address,
                Memo = "Test get resource"
            });

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeFalse();
        result.WritePaths.Count.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task ACS2_GetResourceInfo_TransferFrom_NFT_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.TransferFrom),
            new TransferFromInput
            {
                Amount = 100,
                Symbol = "ABC-1",
                From = Accounts[1].Address,
                To = Accounts[2].Address,
                Memo = "Test get resource"
            });

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeFalse();
        result.WritePaths.Count.ShouldBeGreaterThan(0);
    }

    private async Task<Address> GetDefaultParliamentAddressAsync()
    {
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        return defaultParliamentAddress;
    }

    [Fact]
    public async Task ACS2_GetResourceInfo_Transfer_MultiToken_Test()
    {
        var theDefaultController = await GetDefaultParliamentAddressAsync();
        var newSymbolList = new SymbolListToPayTxSizeFee();
        await CreateTokenAsync(DefaultAddress, "CPU");
        await CreateTokenAsync(DefaultAddress, "NET");
        var methodName = "Transfer";
        var tokenSymbol = "NET";
        var basicFee = 100;
        var methodFeeController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
        var proposalMethodName = nameof(TokenContractStub.SetMethodFee);
        var methodFees = new MethodFees
        {
            MethodName = methodName,
            Fees =
            {
                new MethodFee { Symbol = tokenSymbol, BasicFee = basicFee }
            }
        };
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            methodFeeController.OwnerAddress, proposalMethodName, methodFees);
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = "ELF",
            AddedTokenWeight = 1,
            BaseTokenWeight = 1
        });
        newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
        {
            TokenSymbol = "CPU",
            AddedTokenWeight = 2,
            BaseTokenWeight = 1
        });
        var createProposalInput = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            Params = newSymbolList.ToByteString(),
            OrganizationAddress = theDefaultController,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .SetSymbolsToPayTxSizeFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        };
        var parliamentCreateProposal = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
        var parliamentProposalId = parliamentCreateProposal.Output;
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(parliamentProposalId);
            approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        }
        var transactionRelease = ParliamentContractStub.Release.GetTransaction(parliamentProposalId);
        await ExecuteTransactionWithMiningAsync(transactionRelease);
        var symbolListToPayTxSizeFee = await TokenContractStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
        symbolListToPayTxSizeFee.SymbolsToPayTxSizeFee.Count.ShouldBeGreaterThan(0);
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.Transfer),
            new TransferFromInput
            {
                Amount = 100,
                Symbol = "ELF",
                From = Accounts[1].Address,
                To = Accounts[2].Address,
                Memo = "Test get resource"
            });

        var result1 = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result1.NonParallelizable.ShouldBeFalse();
        result1.WritePaths.Count.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public async Task ACS2_GetResourceInfo_TransferFrom_WithDelegate_Test()
    {
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = TokenContractAddress,
            MethodName = nameof(TokenContractStub.TransferFrom),
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        await TokenContractStubUser.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = Accounts[0].Address,
            DelegateInfoList = { delegateInfo1 }
        });
        await TokenContractStubDelegate.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = Accounts[0].Address,
            DelegateInfoList = { delegateInfo1 }
        });
        await TokenContractStubDelegate2.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1 }
        });
        await TokenContractStubDelegate3.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User2Address,
            DelegateInfoList = { delegateInfo1 }
        });
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.TransferFrom),
            new TransferFromInput
            {
                Amount = 100,
                Symbol = NativeToken,
                From = Accounts[1].Address,
                To = Accounts[2].Address,
                Memo = "Test get resource"
            });

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeFalse();
        result.WritePaths.Count.ShouldBe(10);

    }
    [Fact]
    public async Task ACS2_GetResourceInfo_DonateResourceToken_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.DonateResourceToken),
            new Empty());

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeTrue();
    }

    [Fact]
    public async Task ACS2_GetResourceInfo_ClaimTransactionFees_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.ClaimTransactionFees),
            new Empty());

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeTrue();
    }

    [Fact]
    public async Task ACS2_GetResourceInfo_UnsupportedMethod_Test()
    {
        var transaction = GenerateTokenTransaction(Accounts[0].Address, "TestMethod",
            new Empty());

        var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
        result.NonParallelizable.ShouldBeTrue();
    }
}