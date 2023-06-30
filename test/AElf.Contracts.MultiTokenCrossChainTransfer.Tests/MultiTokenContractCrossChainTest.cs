using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.CSharp.Core.Utils;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public class MultiTokenContractCrossChainTest : MultiTokenContractCrossChainTestBase
{
    private const string SymbolForTesting = "ELFTEST";
    private const string NativeToken = "ELF";
    private static readonly long _totalSupply = 1000L;
    private readonly Hash _fakeBlockHeader = HashHelper.ComputeFrom("fakeBlockHeader");
    private readonly int _parentChainHeightOfCreation = 5;
    private readonly string sideChainSymbol = "STA";

    #region register test

    [Fact]
    public async Task MainChain_RegisterCrossChainTokenContractAddress_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        //Side chain validate transaction
        var validateTransaction = SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = SideTokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var sideBlock = await SideChainTestKit.MineAsync(new List<Transaction> { validateTransaction });
        var validateTransactionResult = sideBlock.TransactionResultMap[validateTransaction.GetHash()];
        validateTransactionResult.Status.ShouldBe(TransactionResultStatus.Mined, validateTransactionResult.Error);

        //Main chain side chain index each other
        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

        // Main chain register
        var result = await RegisterSideChainTokenContractAsync(TokenContractAddress, validateTransaction,
            merklePath,
            boundParentChainHeightAndMerklePath, sideChainId);
        result.Status.ShouldBe(TransactionResultStatus.Mined, result.Error);
    }

    [Fact]
    public async Task SideChain_RegisterCrossChainTokenContractAddress_Test()
    {
        await GenerateSideChainAsync();
        //Main chain validate transaction
        var validateTransaction = BasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = TokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var blockExecutedSet = await MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = blockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);
        // Index main chain
        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(_parentChainHeightOfCreation, blockRoot, blockRoot);
        //Side chain register
        var result =
            await RegisterMainChainTokenContractOnSideChainAsync(validateTransaction, merklePath,
                _parentChainHeightOfCreation);
        Assert.True(result.Status == TransactionResultStatus.Mined, result.Error);
    }

    [Fact]
    public async Task RegisterCrossChainTokenContractAddress_WithoutPermission_Test()
    {
        var validateTransaction = BasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = TokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var executionResult = await TokenContractStub.RegisterCrossChainTokenContractAddress.SendWithExceptionAsync(
            new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = _parentChainHeightOfCreation,
                TokenContractAddress = TokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            });
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        executionResult.TransactionResult.Error.ShouldContain("No permission.");
    }

    [Fact]
    public async Task RegisterCrossChainTokenContractAddress_ValidateWrongAddress_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        var validateTransaction = SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = SideCrossChainContractAddress,
                SystemContractHashName = CrossChainSmartContractAddressNameProvider.Name
            });
        var sideBlockExecutedSet = await SideChainTestKit.MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = sideBlockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);

        //Main chain side chain index each other
        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await MainAndSideIndexAsync(sideChainId, sideBlockExecutedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(sideBlockExecutedSet.Height);

        // Main chain register
        var result = await RegisterSideChainTokenContractAsync(TokenContractAddress, validateTransaction,
            merklePath,
            boundParentChainHeightAndMerklePath, sideChainId);

        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain("Address validation failed.");
    }

    [Fact]
    public async Task RegisterCrossChainTokenContractAddress_InvalidTransaction_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        var wrongChainId = ChainHelper.GetChainId(1);
        var validateTransaction = SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = SideTokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var sideBlockExecutedSet = await SideChainTestKit.MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = sideBlockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);

        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await MainAndSideIndexAsync(sideChainId, sideBlockExecutedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(sideBlockExecutedSet.Height);

        var result = await RegisterSideChainTokenContractAsync(TokenContractAddress, validateTransaction,
            merklePath,
            boundParentChainHeightAndMerklePath, wrongChainId);
        Assert.True(result.Status == TransactionResultStatus.Failed);
        result.Error.ShouldContain("Invalid transaction.");
    }

    [Fact]
    public async Task RegisterCrossChainTokenContractAddress_VerificationFailed_Test()
    {
        await GenerateSideChainAsync();
        //Main chain validate transaction
        var validateTransaction = BasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = TokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var blockExecutedSet = await MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = blockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);
        // Index main chain
        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(_parentChainHeightOfCreation, blockRoot, blockRoot);
        // Wrong merklePath
        merklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);
        //Side chain register
        var result =
            await RegisterMainChainTokenContractOnSideChainAsync(validateTransaction, merklePath,
                _parentChainHeightOfCreation);
        Assert.True(result.Status == TransactionResultStatus.Failed);
        Assert.Contains("Cross chain verification failed.", result.Error);
    }

    #endregion

    #region cross chain create token test

    [Fact]
    public async Task MainChain_CrossChainCreateToken_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        // Side chain create token
        var createTransaction =
            await CreateTransactionForTokenCreation(SideChainTokenContractStub, SideChainTestKit.DefaultAccount.Address,
                SymbolForTesting, SideTokenContractAddress);
        var executedSet = await SideChainTestKit.MineAsync(new List<Transaction> { createTransaction });
        var createResult = executedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);

        var createdTokenInfo = await SideChainTokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var tokenValidationTransaction =
            CreateTokenInfoValidationTransaction(createdTokenInfo, SideChainTokenContractStub);

        executedSet = await SideChainTestKit.MineAsync(new List<Transaction> { tokenValidationTransaction });
        var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        await MainAndSideIndexAsync(sideChainId, executedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(executedSet.Height);

        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = sideChainId,
            ParentChainHeight = boundParentChainHeightAndMerklePath.BoundParentChainHeight,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = merklePath
        };
        crossChainCreateTokenInput.MerklePath.MerklePathNodes.AddRange(boundParentChainHeightAndMerklePath
            .MerklePathFromParentChain.MerklePathNodes);
        // Main chain cross chain create
        await TokenContractStub.CrossChainCreateToken.SendAsync(crossChainCreateTokenInput);
    }

    [Fact]
    public async Task SideChain_CrossChainCreateToken_Test()
    {
        await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();

        // Main chain create token
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress);
        var blockExecutedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = blockExecutedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);

        var createdTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var tokenValidationTransaction = CreateTokenInfoValidationTransaction(createdTokenInfo,
            TokenContractStub);

        blockExecutedSet = await MineAsync(new List<Transaction> { tokenValidationTransaction });
        var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(blockExecutedSet.Height, blockRoot, blockRoot);
        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = blockExecutedSet.Height,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = merklePath
        };
        // Side chain cross chain create
        var executionResult =
            await SideChainTokenContractStub.CrossChainCreateToken.SendAsync(crossChainCreateTokenInput);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
            executionResult.TransactionResult.Error);

        var newTokenInfo = await SideChainTokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        newTokenInfo.TotalSupply.ShouldBe(_totalSupply);
    }

    [Fact]
    public async Task SideChain_CrossChainSideChainCreateToken_Test()
    {
        var sideChainId1 = await GenerateSideChainAsync();
        await GenerateSideChain2Async();
        await RegisterSideChainContractAddressOnSideChainAsync(sideChainId1);

        // side chain 1 valid token
        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var createdTokenInfo = await SideChainTokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = sideChainSymbol
        });
        var tokenValidationTransaction =
            CreateTokenInfoValidationTransaction(createdTokenInfo, SideChainTokenContractStub);

        var blockExecutedSet = await SideChainTestKit.MineAsync(new List<Transaction> { tokenValidationTransaction });
        var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
        await SideIndexSideChainAsync(sideChainId1, blockExecutedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(blockExecutedSet.Height);

        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = sideChainId1,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = merklePath
        };
        crossChainCreateTokenInput.MerklePath.MerklePathNodes.AddRange(boundParentChainHeightAndMerklePath
            .MerklePathFromParentChain.MerklePathNodes);
        crossChainCreateTokenInput.ParentChainHeight = boundParentChainHeightAndMerklePath.BoundParentChainHeight;

        // Side chain 2 cross chain create
        var executionResult =
            await SideChain2TokenContractStub.CrossChainCreateToken.SendAsync(crossChainCreateTokenInput);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
            executionResult.TransactionResult.Error);

        var newTokenInfo = await SideChain2TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = sideChainSymbol
        });
        Assert.True(newTokenInfo.TotalSupply == createdTokenInfo.TotalSupply);
        Assert.True(newTokenInfo.Issuer == createdTokenInfo.Issuer);
        Assert.True(newTokenInfo.IssueChainId == createdTokenInfo.IssueChainId);
        Assert.True(newTokenInfo.Symbol == createdTokenInfo.Symbol);
    }


    [Fact]
    public async Task SideChain_CrossChainCreateToken_WithAlreadyCreated_Test()
    {
        await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();

        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress);
        var blockExecutedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = blockExecutedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);
        var sideCreateTransaction = await CreateTransactionForTokenCreation(SideChainTokenContractStub,
            SideChainTestKit.DefaultAccount.Address, SymbolForTesting, SideTokenContractAddress);
        blockExecutedSet = await SideChainTestKit.MineAsync(new List<Transaction> { sideCreateTransaction });
        var sideCreateResult = blockExecutedSet.TransactionResultMap[sideCreateTransaction.GetHash()];
        Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined, sideCreateResult.Error);

        var createdTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var tokenValidationTransaction = CreateTokenInfoValidationTransaction(createdTokenInfo, TokenContractStub);
        var executedSet = await MineAsync(new List<Transaction> { tokenValidationTransaction });
        var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(executedSet.Height, blockRoot, blockRoot);
        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = executedSet.Height,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = merklePath
        };

        var executionResult =
            await SideChainTokenContractStub.CrossChainCreateToken.SendWithExceptionAsync(
                crossChainCreateTokenInput);
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Token already exists.", executionResult.TransactionResult.Error);
    }

    [Fact]
    public async Task CrossChainCreateToken_With_Invalid_Verification_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        var createdTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var tokenValidationTransaction = CreateTokenInfoValidationTransaction(createdTokenInfo, TokenContractStub);
        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = sideChainId,
            ParentChainHeight = 0,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = new MerklePath()
        };
        // Main chain cross chain create
        var result =
            (await TokenContractStub.CrossChainCreateToken.SendWithExceptionAsync(crossChainCreateTokenInput))
            .TransactionResult;
        Assert.True(result.Status == TransactionResultStatus.Failed);
        Assert.Contains("Invalid transaction", result.Error);
    }

    #endregion

    #region cross chain transfer

    [Fact]
    public async Task MainChain_CrossChainTransfer_NativeToken_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAccount.Address,
            Symbol = NativeToken
        });

        //Main chain cross transfer to side chain
        var executionResult = await TokenContractStub.CrossChainTransfer.SendAsync(new CrossChainTransferInput
        {
            Symbol = NativeToken,
            ToChainId = sideChainId,
            Amount = 1000,
            To = SideChainTestKit.DefaultAccount.Address,
            IssueChainId = MainChainId
        });
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Mined,
            executionResult.TransactionResult.Error);

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAccount.Address,
            Symbol = NativeToken
        });
        balance.Balance.ShouldBe(balanceBefore.Balance - 1000);

        //verify side chain token address throw main chain token contract
        var tokenAddress = await TokenContractStub.GetCrossChainTransferTokenContractAddress.CallAsync(
            new GetCrossChainTransferTokenContractAddressInput
            {
                ChainId = sideChainId
            });
        tokenAddress.ShouldBe(SideTokenContractAddress);
    }

    [Fact]
    public async Task MainChain_CrossChainTransfer_Without_Burnable_Token_Test()
    {
        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress, false);
        var blockExecutedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = blockExecutedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);
        var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var issueId = tokenInfo.IssueChainId;
        var crossChainTransferTransaction = TokenContractStub.CrossChainTransfer.GetTransaction(
            new CrossChainTransferInput
            {
                Symbol = SymbolForTesting,
                ToChainId = issueId,
                Amount = 1000,
                To = DefaultAccount.Address,
                IssueChainId = issueId
            });
        blockExecutedSet = await MineAsync(new List<Transaction> { crossChainTransferTransaction });
        var txResult2 = blockExecutedSet.TransactionResultMap[crossChainTransferTransaction.GetHash()];
        txResult2.Error.ShouldContain("The token is not burnable");
    }


    [Fact]
    public async Task MainChain_CrossChainTransfer_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        // Main chain create token
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress);
        var executedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = executedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);

        var createdTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = SymbolForTesting
        });
        var tokenValidationTransaction = TokenContractStub.ValidateTokenInfoExists.GetTransaction(
            new ValidateTokenInfoExistsInput
            {
                TokenName = createdTokenInfo.TokenName,
                Symbol = createdTokenInfo.Symbol,
                Decimals = createdTokenInfo.Decimals,
                Issuer = createdTokenInfo.Issuer,
                IsBurnable = createdTokenInfo.IsBurnable,
                TotalSupply = createdTokenInfo.TotalSupply,
                IssueChainId = createdTokenInfo.IssueChainId
            });
        executedSet = await MineAsync(new List<Transaction> { tokenValidationTransaction });
        var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(executedSet.Height, blockRoot, blockRoot);
        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = executedSet.Height,
            TransactionBytes = tokenValidationTransaction.ToByteString(),
            MerklePath = merklePath
        };
        // Side chain cross chain create
        await SideChainTokenContractStub.CrossChainCreateToken.SendAsync(crossChainCreateTokenInput);

        //Main chain cross transfer to side chain
        await IssueTransactionAsync(SymbolForTesting, 1000);
        var executionResult = await TokenContractStub.CrossChainTransfer.SendAsync(new CrossChainTransferInput
        {
            Symbol = SymbolForTesting,
            ToChainId = sideChainId,
            Amount = 1000,
            To = SideChainTestKit.DefaultAccount.Address,
            IssueChainId = MainChainId
        });
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
            executionResult.TransactionResult.Error);
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAccount.Address,
            Symbol = SymbolForTesting
        });
        balance.Balance.ShouldBe(0);

        // can't issue Token on chain which is not issue chain
        executionResult = await SideChainTokenContractStub.Issue.SendWithExceptionAsync(new IssueInput
        {
            Symbol = SymbolForTesting,
            Amount = _totalSupply,
            To = SideChainTestKit.DefaultAccount.Address
        });
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Unable to issue token with wrong chainId", executionResult.TransactionResult.Error);
    }

    [Fact]
    public async Task MainChain_CrossChainTransfer_IncorrectChainId_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        var executionResult = await TokenContractStub.CrossChainTransfer.SendWithExceptionAsync(
            new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = sideChainId
            });
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Incorrect issue chain id.", executionResult.TransactionResult.Error);
    }

    [Fact]
    public async Task MainChain_CrossChainTransfer_InvalidChainId_Test()
    {
        var wrongSideChainId = ChainHelper.GetChainId(1);
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        await TokenContractStub.CrossChainTransfer.SendAsync(
            new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = wrongSideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId
            });
    }

    [Fact]
    public async Task MainChain_CrossChainTransfer_MemoLength_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        {
            await TokenContractStub.CrossChainTransfer.SendAsync(new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId,
                Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest.."
            });
        }

        {
            var executionResult = await TokenContractStub.CrossChainTransfer.SendWithExceptionAsync(
                new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTestKit.DefaultAccount.Address,
                    IssueChainId = MainChainId,
                    Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest..."
                });
            Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid memo size.", executionResult.TransactionResult.Error);
        }
    }

    #endregion

    #region cross chain receive

    [Fact]
    public async Task SideChain_CrossChainReceived_NativeToken_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        //Main chain cross transfer to side chain
        var crossChainTransferTransaction = TokenContractStub.CrossChainTransfer.GetTransaction(
            new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId
            });
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var blockExecutedSet = await MineAsync(new List<Transaction> { crossChainTransferTransaction });
        var txResult = blockExecutedSet.TransactionResultMap[crossChainTransferTransaction.GetHash()];
        txResult.Status.ShouldBe(TransactionResultStatus.Mined, txResult.Error);

        var height = blockExecutedSet.Height > _parentChainHeightOfCreation
            ? blockExecutedSet.Height
            : _parentChainHeightOfCreation;
        var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(height, blockRoot, blockRoot);
        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = height,
            TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
            MerklePath = transferMerKlePath
        };
        var receiveResult =
            await SideChainTokenContractStub.CrossChainReceiveToken.SendAsync(crossChainReceiveTokenInput);

        var logEvent = receiveResult.TransactionResult.Logs.First(l => l.Name == nameof(CrossChainReceived));
        var receivedEvent = new CrossChainReceived();
        receivedEvent.MergeFrom(logEvent.NonIndexed);
        receivedEvent.From.ShouldBe(SideChainTestKit.DefaultAccount.Address);
        receivedEvent.To.ShouldBe(SideChainTestKit.DefaultAccount.Address);
        receivedEvent.Symbol.ShouldBe(NativeToken);
        receivedEvent.Amount.ShouldBe(1000);
        receivedEvent.Memo.ShouldBeEmpty();
        receivedEvent.FromChainId.ShouldBe(MainChainId);
        receivedEvent.ParentChainHeight.ShouldBe(height);
        receivedEvent.IssueChainId.ShouldBe(MainChainId);
        receivedEvent.TransferTransactionId.ShouldBe(crossChainTransferTransaction.GetHash());

        var output = await SideChainTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = SideChainTestKit.DefaultAccount.Address,
            Symbol = NativeToken
        });
        Assert.Equal(1000, output.Balance);
    }

    [Fact]
    public async Task SideChain_CrossChainReceived_Twice_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        //Main chain cross transfer to side chain
        var crossChainTransferTransaction = TokenContractStub.CrossChainTransfer.GetTransaction(
            new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId
            });
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var blockExecutedSet = await MineAsync(new List<Transaction> { crossChainTransferTransaction });
        var txResult = blockExecutedSet.TransactionResultMap[crossChainTransferTransaction.GetHash()];
        txResult.Status.ShouldBe(TransactionResultStatus.Mined, txResult.Error);

        var height = blockExecutedSet.Height > _parentChainHeightOfCreation
            ? blockExecutedSet.Height
            : _parentChainHeightOfCreation;
        var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(height, blockRoot, blockRoot);
        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = height,
            TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
            MerklePath = transferMerKlePath
        };
        await SideChainTokenContractStub.CrossChainReceiveToken.SendAsync(crossChainReceiveTokenInput);

        var executionResult =
            await SideChainTokenContractStub.CrossChainReceiveToken.SendWithExceptionAsync(
                crossChainReceiveTokenInput);
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Token already claimed.", executionResult.TransactionResult.Error);
    }

    [Fact]
    public async Task SideChain_CrossChainReceived_InvalidToken_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);
        var transferAmount = 1000;

        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress);
        var executedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = executedSet.TransactionResultMap[createTransaction.GetHash()];
        createResult.Status.ShouldBe(TransactionResultStatus.Mined, createResult.Error);
        await IssueTransactionAsync(SymbolForTesting, transferAmount);

        //Main chain cross transfer to side chain
        var crossChainTransferTransaction = TokenContractStub.CrossChainTransfer.GetTransaction(
            new CrossChainTransferInput
            {
                Symbol = SymbolForTesting,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId
            });
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        var blockExecutedSet = await MineAsync(new List<Transaction> { crossChainTransferTransaction });
        var txResult = blockExecutedSet.TransactionResultMap[crossChainTransferTransaction.GetHash()];
        txResult.Status.ShouldBe(TransactionResultStatus.Mined, txResult.Error);

        var height = blockExecutedSet.Height > _parentChainHeightOfCreation
            ? blockExecutedSet.Height
            : _parentChainHeightOfCreation;
        var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(height, blockRoot, blockRoot);
        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = height,
            TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
            MerklePath = transferMerKlePath
        };
        var executionResult =
            await SideChainTokenContractStub.CrossChainReceiveToken.SendWithExceptionAsync(
                crossChainReceiveTokenInput);

        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Token is not found.", executionResult.TransactionResult.Error);
    }

    [Fact]
    public async Task SideChain_CrossChainReceived_DifferentReceiver_Test()
    {
        var sideChainId = await GenerateSideChainAsync();
        await RegisterSideChainContractAddressOnMainChainAsync();
        await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

        var crossChainTransferTransaction = TokenContractStub.CrossChainTransfer.GetTransaction(
            new CrossChainTransferInput
            {
                Symbol = NativeToken,
                ToChainId = sideChainId,
                Amount = 1000,
                To = SideChainTestKit.DefaultAccount.Address,
                IssueChainId = MainChainId
            });

        var blockExecutedSet = await MineAsync(new List<Transaction> { crossChainTransferTransaction });
        var txResult = blockExecutedSet.TransactionResultMap[crossChainTransferTransaction.GetHash()];
        txResult.Status.ShouldBe(TransactionResultStatus.Mined, txResult.Error);

        var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(blockExecutedSet.Height, blockRoot, blockRoot);
        var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = blockExecutedSet.Height,
            TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
            MerklePath = transferMerKlePath
        };

        var tokenContractImplStub = SideChainTestKit.GetTester<TokenContractImplContainer.TokenContractImplStub>(
            SideTokenContractAddress,
            SampleAccount.Accounts[2].KeyPair);
        var executionResult =
            await tokenContractImplStub.CrossChainReceiveToken.SendAsync(crossChainReceiveTokenInput);
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task CrossChainCreateToken_WithoutRegister_Test()
    {
        await GenerateSideChainAsync(false);
        var createTransaction = await CreateTransactionForTokenCreation(TokenContractStub,
            DefaultAccount.Address, SymbolForTesting, TokenContractAddress);
        var blockExecutedSet = await MineAsync(new List<Transaction> { createTransaction });
        var createResult = blockExecutedSet.TransactionResultMap[createTransaction.GetHash()];
        Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);

        var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
        await IndexMainChainTransactionAsync(_parentChainHeightOfCreation, blockRoot, blockRoot);
        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = _parentChainHeightOfCreation,
            TransactionBytes = createTransaction.ToByteString(),
            MerklePath = merklePath
        };

        var executionResult =
            await SideChainTokenContractStub.CrossChainCreateToken.SendWithExceptionAsync(
                crossChainCreateTokenInput);
        Assert.True(executionResult.TransactionResult.Status == TransactionResultStatus.Failed);
        Assert.Contains("Token contract address of chain AELF not registered.",
            executionResult.TransactionResult.Error);
    }

    #endregion

    #region private method

    private async Task<int> GenerateSideChainAsync(bool registerParentChainTokenContractAddress = true)
    {
        var sideChainId =
            await InitAndCreateSideChainAsync(sideChainSymbol, _parentChainHeightOfCreation, MainChainId, 100);
        StartSideChain(sideChainId, _parentChainHeightOfCreation, sideChainSymbol,
            registerParentChainTokenContractAddress);
        return sideChainId;
    }

    private async Task GenerateSideChain2Async()
    {
        var sideChainId = await InitAndCreateSideChainAsync("STB", _parentChainHeightOfCreation, MainChainId, 100);
        StartSideChain2(sideChainId, _parentChainHeightOfCreation, "STB");
    }

    private Transaction ValidateTransaction(Address tokenContractAddress, Hash name, bool isMainChain)
    {
        if (isMainChain)
            return BasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
                new ValidateSystemContractAddressInput
                {
                    Address = tokenContractAddress,
                    SystemContractHashName = name
                });

        return SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = tokenContractAddress,
                SystemContractHashName = name
            });
    }


    private async Task<Transaction> CreateTransactionForTokenCreation(
        TokenContractImplContainer.TokenContractImplStub tokenContractImplStub,
        Address issuer, string symbol, Address lockWhiteAddress, bool isBurnable = true)
    {
        await CreateSeedNftCollection(tokenContractImplStub, issuer);
        var tokenInfo = GetTokenInfo(symbol, issuer, isBurnable);
        var input = new CreateInput
        {
            Symbol = tokenInfo.Symbol,
            Decimals = tokenInfo.Decimals,
            Issuer = tokenInfo.Issuer,
            IsBurnable = tokenInfo.IsBurnable,
            TokenName = tokenInfo.TokenName,
            TotalSupply = tokenInfo.TotalSupply
        };
        await CreateSeedNftAsync(tokenContractImplStub, input, lockWhiteAddress);
        return tokenContractImplStub.Create.GetTransaction(input);
    }


    private Transaction CreateTokenInfoValidationTransaction(TokenInfo createdTokenInfo,
        TokenContractImplContainer.TokenContractImplStub tokenContractImplStub)
    {
        return tokenContractImplStub.ValidateTokenInfoExists.GetTransaction(new ValidateTokenInfoExistsInput
        {
            TokenName = createdTokenInfo.TokenName,
            Symbol = createdTokenInfo.Symbol,
            Decimals = createdTokenInfo.Decimals,
            Issuer = createdTokenInfo.Issuer,
            IsBurnable = createdTokenInfo.IsBurnable,
            TotalSupply = createdTokenInfo.TotalSupply,
            IssueChainId = createdTokenInfo.IssueChainId
        });
    }

    private TokenInfo GetTokenInfo(string symbol, Address issuer, bool isBurnable = true)
    {
        return new TokenInfo
        {
            Symbol = symbol,
            Decimals = 2,
            Issuer = issuer,
            IsBurnable = isBurnable,
            TokenName = "Symbol for testing",
            TotalSupply = 1000
        };
    }

    private async Task IssueTransactionAsync(string symbol, long amount)
    {
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = symbol,
            To = DefaultAccount.Address,
            Amount = amount
        });
        var output = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAccount.Address,
            Symbol = symbol
        });

        output.Balance.ShouldBe(amount);
    }

    private async Task RegisterMainChainTokenContractAddressOnSideChainAsync(int sideChainId)
    {
        var validateTransaction = SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = SideTokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var sideBlockExecutedSet = await SideChainTestKit.MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = sideBlockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);

        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        await MainAndSideIndexAsync(sideChainId, sideBlockExecutedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(sideBlockExecutedSet.Height);

        //await BootMinerChangeRoundAsync(true);
        var result = await RegisterSideChainTokenContractAsync(TokenContractAddress, validateTransaction,
            merklePath,
            boundParentChainHeightAndMerklePath, sideChainId);
        result.Status.ShouldBe(TransactionResultStatus.Mined, result.Error);
    }

    private async Task RegisterSideChainContractAddressOnMainChainAsync()
    {
        var validateTransaction = BasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = TokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var blockExecutedSet = await MineAsync(new List<Transaction> { validateTransaction });
        var validateResult = blockExecutedSet.TransactionResultMap[validateTransaction.GetHash()];
        Assert.True(validateResult.Status == TransactionResultStatus.Mined, validateResult.Error);
        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
        var height = blockExecutedSet.Height > _parentChainHeightOfCreation
            ? blockExecutedSet.Height
            : _parentChainHeightOfCreation;
        await IndexMainChainTransactionAsync(height, blockRoot, blockRoot);
        var result = await RegisterMainChainTokenContractOnSideChainAsync(validateTransaction, merklePath, height);
        result.Status.ShouldBe(TransactionResultStatus.Mined, result.Error);
    }

    private async Task RegisterSideChainContractAddressOnSideChainAsync(int sideChainId)
    {
        var validateTransaction1 = SideChainBasicContractZeroStub.ValidateSystemContractAddress.GetTransaction(
            new ValidateSystemContractAddressInput
            {
                Address = SideTokenContractAddress,
                SystemContractHashName = TokenSmartContractAddressNameProvider.Name
            });
        var executedSet = await SideChainTestKit.MineAsync(new List<Transaction> { validateTransaction1 });
        var validateResult = executedSet.TransactionResultMap[validateTransaction1.GetHash()];
        validateResult.Status.ShouldBe(TransactionResultStatus.Mined, validateResult.Error);

        var merklePath = GetTransactionMerklePathAndRoot(validateTransaction1, out var blockRoot);
        await SideIndexSideChainAsync(sideChainId, executedSet.Height, blockRoot);
        var boundParentChainHeightAndMerklePath =
            await GetBoundParentChainHeightAndMerklePathByHeight(executedSet.Height);

        var result = await RegisterSideChainTokenContractOnSieChain2Async(Side2TokenContractAddress,
            validateTransaction1, merklePath,
            boundParentChainHeightAndMerklePath, sideChainId);
        result.Status.ShouldBe(TransactionResultStatus.Mined, result.Error);
    }

    private async Task<TransactionResult> RegisterMainChainTokenContractOnSideChainAsync(Transaction transaction,
        MerklePath merklePath,
        long height)
    {
        var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
        {
            FromChainId = MainChainId,
            ParentChainHeight = height,
            TokenContractAddress = TokenContractAddress,
            TransactionBytes = transaction.ToByteString(),
            MerklePath = merklePath
        };
        var proposalId = await CreateProposalAsync(SideChainParliamentContractStub,
            nameof(TokenContractImplContainer.TokenContractImplStub.RegisterCrossChainTokenContractAddress),
            registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
        await ApproveWithMinersAsync(proposalId, false);
        return await SideChainTestKit.ExecuteTransactionWithMiningAsync(
            SideChainParliamentContractStub.Release.GetTransaction(proposalId));
        ;
    }

    private async Task<TransactionResult> RegisterSideChainTokenContractAsync(Address tokenContract,
        IMessage transaction,
        MerklePath merklePath,
        CrossChainMerkleProofContext boundParentChainHeightAndMerklePath, int sideChainId)
    {
        var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
        {
            FromChainId = sideChainId,
            ParentChainHeight = boundParentChainHeightAndMerklePath.BoundParentChainHeight,
            TokenContractAddress = SideTokenContractAddress,
            TransactionBytes = transaction.ToByteString(),
            MerklePath = merklePath
        };
        registerCrossChainTokenContractAddressInput.MerklePath.MerklePathNodes.AddRange(
            boundParentChainHeightAndMerklePath.MerklePathFromParentChain.MerklePathNodes);
        var proposalId = await CreateProposalAsync(ParliamentContractStub,
            nameof(TokenContractImplContainer.TokenContractImplStub.RegisterCrossChainTokenContractAddress),
            registerCrossChainTokenContractAddressInput.ToByteString(), tokenContract);
        await ApproveWithMinersAsync(proposalId);
        return await ReleaseProposalAsync(proposalId);
    }

    private async Task<TransactionResult> RegisterSideChainTokenContractOnSieChain2Async(Address tokenContract,
        IMessage transaction,
        MerklePath merklePath,
        CrossChainMerkleProofContext boundParentChainHeightAndMerklePath, int sideChainId)
    {
        var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
        {
            FromChainId = sideChainId,
            ParentChainHeight = boundParentChainHeightAndMerklePath.BoundParentChainHeight,
            TokenContractAddress = SideTokenContractAddress,
            TransactionBytes = transaction.ToByteString(),
            MerklePath = merklePath
        };
        registerCrossChainTokenContractAddressInput.MerklePath.MerklePathNodes.AddRange(
            boundParentChainHeightAndMerklePath.MerklePathFromParentChain.MerklePathNodes);
        var proposalId = await CreateProposalAsync(SideChain2ParliamentContractStub,
            nameof(TokenContractImplContainer.TokenContractImplStub.RegisterCrossChainTokenContractAddress),
            registerCrossChainTokenContractAddressInput.ToByteString(), tokenContract);
        await ApproveWithMinersAsync(proposalId);
        var transactionList = new List<Transaction>();
        foreach (var account in SampleAccount.Accounts.Take(5))
        {
            var parliamentContractStub =
                SideChain2TestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                    Side2ParliamentAddress, account.KeyPair);
            transactionList.Add(parliamentContractStub.Approve.GetTransaction(proposalId));
        }

        await SideChain2TestKit.MineAsync(transactionList);
        var releaseTransaction = SideChain2ParliamentContractStub.Release.GetTransaction(proposalId);
        return await SideChain2TestKit.ExecuteTransactionWithMiningAsync(releaseTransaction);
    }

    private MerklePath GetTransactionMerklePathAndRoot(Transaction transaction, out Hash root)
    {
        var fakeHash1 = HashHelper.ComputeFrom("fake1");
        var fakeHash2 = HashHelper.ComputeFrom("fake2");

        var rawBytes = transaction.GetHash().ToByteArray()
            .Concat(EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString())).ToArray();
        var hash = HashHelper.ComputeFrom(rawBytes);
        var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] { hash, fakeHash1, fakeHash2 });

        var merklePath = binaryMerkleTree.GenerateMerklePath(0);
        root = binaryMerkleTree.Root;
        return merklePath;
    }

    private async Task IndexMainChainTransactionAsync(long height, Hash txRoot, Hash blockRoot)
    {
        var indexParentHeight = await GetParentChainHeight(SideChainCrossChainContractStub);
        var crossChainBlockData = new CrossChainBlockData();
        var index = indexParentHeight >= _parentChainHeightOfCreation
            ? indexParentHeight + 1
            : _parentChainHeightOfCreation;
        for (var i = index; i < height; i++)
            crossChainBlockData.ParentChainBlockDataList.Add(CreateParentChainBlockData(i, MainChainId,
                txRoot));

        var parentChainBlockData = CreateParentChainBlockData(height, MainChainId, txRoot);
        crossChainBlockData.ParentChainBlockDataList.Add(parentChainBlockData);

        parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
        {
            TransactionStatusMerkleTreeRoot = blockRoot
        };

        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var proposingResult =
            await SideChainCrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposingResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed)
            .ProposalId;
        await ApproveWithMinersAsync(proposalId, false);
        var releaseResult =
            await SideChainCrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput { ChainIdList = { MainChainId } });
        var releasedProposalId = ProposalReleased.Parser
            .ParseFrom(releaseResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalReleased)))
                .NonIndexed).ProposalId;
        releasedProposalId.ShouldBe(proposalId);

        var parentChainHeight = await GetParentChainHeight(SideChainCrossChainContractStub);
        parentChainHeight.ShouldBe(height);
    }

    private async Task IndexMainChainTransactionOnSideChain2Async(long height, Hash txRoot, Hash blockRoot)
    {
        var indexParentHeight = await GetParentChainHeight(SideChain2CrossChainContractStub);
        var crossChainBlockData = new CrossChainBlockData();
        var index = indexParentHeight >= _parentChainHeightOfCreation
            ? indexParentHeight + 1
            : _parentChainHeightOfCreation;
        for (var i = index; i < height; i++)
            crossChainBlockData.ParentChainBlockDataList.Add(CreateParentChainBlockData(i, MainChainId,
                txRoot));

        var parentChainBlockData = CreateParentChainBlockData(height, MainChainId, txRoot);
        crossChainBlockData.ParentChainBlockDataList.Add(parentChainBlockData);

        parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
        {
            TransactionStatusMerkleTreeRoot = blockRoot
        };

        await BootMinerChangeRoundAsync(SideChain2AEDPoSContractStub, false);
        var proposingResult =
            await SideChain2CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposingResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed)
            .ProposalId;

        var transactionList = new List<Transaction>();
        foreach (var account in SampleAccount.Accounts.Take(4))
        {
            var parliamentContractStub =
                SideChain2TestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                    Side2ParliamentAddress, account.KeyPair);
            transactionList.Add(parliamentContractStub.Approve.GetTransaction(proposalId));
        }

        await SideChain2TestKit.MineAsync(transactionList);

        var releaseResult =
            await SideChain2CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput { ChainIdList = { MainChainId } });
        var releasedProposalId = ProposalReleased.Parser
            .ParseFrom(releaseResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalReleased)))
                .NonIndexed).ProposalId;
        releasedProposalId.ShouldBe(proposalId);

        var parentChainHeight = await GetParentChainHeight(SideChain2CrossChainContractStub);
        parentChainHeight.ShouldBe(height);
    }

    private async Task DoIndexAsync(CrossChainBlockData crossChainBlockData, long sideTransactionHeight,
        int sideChainId)
    {
        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        // proposing tx
        var proposingResult = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);

        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposingResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;

        await ApproveWithMinersAsync(proposalId);
        var releaseTx = CrossChainContractStub.ReleaseCrossChainIndexingProposal.GetTransaction(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });
        var blockExecutedSet = await MineAsync(new List<Transaction> { releaseTx });

        await IndexMainChainBlockAsync(releaseTx, blockExecutedSet.Height, sideTransactionHeight,
            crossChainBlockData.SideChainBlockDataList.Select(b => b.TransactionStatusMerkleTreeRoot).ToList());
    }

    private async Task MainAndSideIndexAsync(int sideChainId, long sideTransactionHeight, Hash root)
    {
        //Main chain index side chain transaction
        var crossChainBlockData = new CrossChainBlockData();
        var mainChainIndexSideChain = await GetSideChainHeight(sideChainId);
        var height = mainChainIndexSideChain > 1 ? mainChainIndexSideChain + 1 : 1;
        for (var i = height; i < sideTransactionHeight; i++)
            crossChainBlockData.SideChainBlockDataList.Add(CreateSideChainBlockData(_fakeBlockHeader, i,
                sideChainId,
                root));

        var sideChainBlockData = CreateSideChainBlockData(_fakeBlockHeader, sideTransactionHeight, sideChainId,
            root);
        crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);

        await DoIndexAsync(crossChainBlockData, sideTransactionHeight, sideChainId);
    }

    private async Task SideIndexSideChainAsync(int sideChainId, long sideTransactionHeight, Hash root)
    {
        //Main chain index side chain transaction
        var crossChainBlockData = new CrossChainBlockData();
        var mainChainIndexSideChain = await GetSideChainHeight(sideChainId);
        var height = mainChainIndexSideChain > 1 ? mainChainIndexSideChain + 1 : 1;
        for (var i = height; i < sideTransactionHeight; i++)
            crossChainBlockData.SideChainBlockDataList.Add(CreateSideChainBlockData(_fakeBlockHeader, i,
                sideChainId,
                root));

        var sideChainBlockData = CreateSideChainBlockData(_fakeBlockHeader, sideTransactionHeight, sideChainId,
            root);
        crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);

        await BootMinerChangeRoundAsync(AEDPoSContractStub, true);
        // proposing tx
        var executionResult = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(executionResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;

        await ApproveWithMinersAsync(proposalId);
        var releaseTx = CrossChainContractStub.ReleaseCrossChainIndexingProposal.GetTransaction(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });
        var executedSet = await MineAsync(new List<Transaction> { releaseTx });

        var blockRoot = await IndexMainChainBlockAsync(releaseTx, executedSet.Height, sideTransactionHeight,
            crossChainBlockData.SideChainBlockDataList.Select(b => b.TransactionStatusMerkleTreeRoot).ToList());

        GetTransactionMerklePathAndRoot(releaseTx, out var txRoot);
        await IndexMainChainTransactionOnSideChain2Async(executedSet.Height, txRoot, blockRoot);
    }

    private async Task<Hash> IndexMainChainBlockAsync(Transaction transaction, long height, long sideHeight,
        List<Hash> indexedSideChainBlockRoots)
    {
        var fakeHash1 = HashHelper.ComputeFrom("fake1");
        var fakeHash2 = HashHelper.ComputeFrom("fake2");

        var rawBytes = transaction.GetHash().ToByteArray()
            .Concat(EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString())).ToArray();
        var hash = HashHelper.ComputeFrom(rawBytes);
        var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] { hash, fakeHash1, fakeHash2 });
        var merkleTreeRootHash = binaryMerkleTree.Root;

        var indexParentHeight = await GetParentChainHeight(SideChainCrossChainContractStub);
        var nextHeightToBeIndexed = indexParentHeight >= _parentChainHeightOfCreation
            ? indexParentHeight + 1
            : _parentChainHeightOfCreation;
        var crossChainBlockData = new CrossChainBlockData();
        for (var i = nextHeightToBeIndexed; i < height; i++)
            crossChainBlockData.ParentChainBlockDataList.Add(
                CreateParentChainBlockData(i, MainChainId, HashHelper.ConcatAndCompute(fakeHash1, fakeHash2)));

        var parentChainBlockData = CreateParentChainBlockData(height, MainChainId,
            merkleTreeRootHash);
        var generatedMerkleTree = BinaryMerkleTree.FromLeafNodes(indexedSideChainBlockRoots);

        parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
        {
            TransactionStatusMerkleTreeRoot = generatedMerkleTree.Root
        };
        parentChainBlockData.IndexedMerklePath.Add(sideHeight,
            generatedMerkleTree.GenerateMerklePath(indexedSideChainBlockRoots.Count - 1));
        crossChainBlockData.ParentChainBlockDataList.Add(parentChainBlockData);

        await BootMinerChangeRoundAsync(SideChainAEDPoSContractStub, false);
        var proposingExecutionResult =
            await SideChainCrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposingExecutionResult.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        await ApproveWithMinersAsync(proposalId, false);
        var releaseExecutionResult =
            await SideChainCrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput { ChainIdList = { MainChainId } });
        var releasedProposalId = ProposalReleased.Parser
            .ParseFrom(releaseExecutionResult.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed).ProposalId;
        releasedProposalId.ShouldBe(proposalId);

        return generatedMerkleTree.Root;
    }

    private ParentChainBlockData CreateParentChainBlockData(long height, int mainChainId, Hash txMerkleTreeRoot)
    {
        return new ParentChainBlockData
        {
            ChainId = mainChainId,
            Height = height,
            TransactionStatusMerkleTreeRoot = txMerkleTreeRoot
        };
    }

    private SideChainBlockData CreateSideChainBlockData(Hash blockHash, long height, int sideChainId,
        Hash txMerkleTreeRoot)
    {
        return new SideChainBlockData
        {
            BlockHeaderHash = blockHash,
            Height = height,
            ChainId = sideChainId,
            TransactionStatusMerkleTreeRoot = txMerkleTreeRoot
        };
    }

    #endregion
}