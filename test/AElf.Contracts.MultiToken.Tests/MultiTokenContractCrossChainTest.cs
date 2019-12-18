using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.CSharp.Core.Utils;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractCrossChainTest : MultiTokenContractCrossChainTestBase
    {
        private const string SymbolForTesting = "ELFTEST";
        private const string NativeToken = "ELF";
        private static long _totalSupply = 1000L;
        private int ParentChainHeightOfCreation = 9;
        private Hash FakeBlockHeader = Hash.FromString("fakeBlockHeader");

        #region register test

        [Fact]
        public async Task MainChain_RegisterCrossChainTokenContractAddress_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            //Side chain validate transaction
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, false);
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            //Main chain side chain index each other
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            // Main chain register
            var result = await DoTokenContractRegistrationOnMainChainAsync(validateTransaction, merklePath,
                boundParentChainHeightAndMerklePath, sideChainId);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task SideChain_RegisterCrossChainTokenContractAddress_Test()
        {
            await GenerateSideChainAsync();
            //Main chain validate transaction
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, true);
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            // Index main chain
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(ParentChainHeightOfCreation, blockRoot);
            //Side chain register
            var result =
                await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath,
                    ParentChainHeightOfCreation);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_WithoutPermission_Test()
        {
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, true);
            var result = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                new RegisterCrossChainTokenContractAddressInput
                {
                    FromChainId = MainChainId,
                    ParentChainHeight = ParentChainHeightOfCreation,
                    TokenContractAddress = TokenContractAddress,
                    TransactionBytes = validateTransaction.ToByteString()
                });
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("No permission.", result.Error);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_ValidateWrongAddress_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideCrossChainContractAddress, CrossChainSmartContractAddressNameProvider.Name, false);
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            //Main chain side chain index each other
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            // Main chain register
            var result = await DoTokenContractRegistrationOnMainChainAsync(validateTransaction, merklePath,
                boundParentChainHeightAndMerklePath, sideChainId);

            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Address validation failed.", result.Error);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_InvalidTransaction_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            var wrongChainId = ChainHelper.GetChainId(1);
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, false);
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            var result = await DoTokenContractRegistrationOnMainChainAsync(validateTransaction, merklePath,
                boundParentChainHeightAndMerklePath, wrongChainId);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid transaction.", result.Error);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_VerificationFailed_Test()
        {
            await GenerateSideChainAsync();
            //Main chain validate transaction
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, true);
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            // Index main chain
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(ParentChainHeightOfCreation, blockRoot);
            // Wrong merklePath
            merklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);
            //Side chain register
            var result =
                await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath,
                    ParentChainHeightOfCreation);
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
            var createTransaction = await CreateTransactionForTokenCreationAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, false);
            await SideChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await SideChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var createdTokenInfoBytes = await SideChainTester.CallContractMethodAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                });
            var createdTokenInfo = TokenInfo.Parser.ParseFrom(createdTokenInfoBytes);
            var tokenValidationTransaction =
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, SideTokenContractAddress, false);

            var sideBlock2 = await SideChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await BootMinerChangeRoundAsync(true);
            await MainAndSideIndexAsync(sideChainId, sideBlock2.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock2.Height);

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
            var result = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_Test()
        {
            await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();

            // Main chain create token
            await BootMinerChangeRoundAsync(true);
            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var createdTokenInfoBytes = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                });
            var createdTokenInfo = TokenInfo.Parser.ParseFrom(createdTokenInfoBytes);
            var tokenValidationTransaction =
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, TokenContractAddress, true);

            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(mainBlock2.Height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = mainBlock2.Height,
                TransactionBytes = tokenValidationTransaction.ToByteString(),
                MerklePath = merklePath
            };
            // Side chain cross chain create
            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var newTokenInfo = TokenInfo.Parser.ParseFrom(await SideChainTester.CallContractMethodAsync(
                SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                }));
            Assert.True(newTokenInfo.TotalSupply == _totalSupply);
        }

        [Fact]
        public async Task CrossChainCreateToken_WithoutRegister_Test()
        {
            await GenerateSideChainAsync();
            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(ParentChainHeightOfCreation, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TransactionBytes = createTransaction.ToByteString(),
                MerklePath = merklePath
            };

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token contract address of chain AELF not registered.", result.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_WithAlreadyCreated_Test()
        {
            await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();

            await BootMinerChangeRoundAsync(true);
            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var sideCreateTransaction = await CreateTransactionForTokenCreationAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, false);
            await SideChainTester.MineAsync(new List<Transaction> {sideCreateTransaction});
            var sideCreateResult = await SideChainTester.GetTransactionResultAsync(sideCreateTransaction.GetHash());
            Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined);

            var createdTokenInfoBytes = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                });
            var createdTokenInfo = TokenInfo.Parser.ParseFrom(createdTokenInfoBytes);
            var tokenValidationTransaction =
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, TokenContractAddress, true);
            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(mainBlock2.Height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = mainBlock2.Height,
                TransactionBytes = tokenValidationTransaction.ToByteString(),
                MerklePath = merklePath
            };

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token already exists.", result.Error);
        }
        
        #endregion

        #region cross chain transfer

        [Fact]
        public async Task MainChain_CrossChainTransfer_NativeToken_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            var balanceBeforeResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput
                {
                    Owner = MainChainTester.GetCallOwnerAddress(),
                    Symbol = NativeToken
                });
            var balanceBefore = GetBalanceOutput.Parser.ParseFrom(balanceBeforeResult).Balance;

            //Main chain cross transfer to side chain
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);

            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var balanceResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput
                {
                    Owner = MainChainTester.GetCallOwnerAddress(),
                    Symbol = NativeToken
                });
            var balance = GetBalanceOutput.Parser.ParseFrom(balanceResult);
            Assert.True(balance.Balance == (balanceBefore - 1000));

            //verify side chain token address throw main chain token contract
            var byteString = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetCrossChainTransferTokenContractAddress),
                new GetCrossChainTransferTokenContractAddressInput
                {
                    ChainId = sideChainId
                });
            var tokenAddress = Address.Parser.ParseFrom(byteString);
            tokenAddress.ShouldBe(SideTokenContractAddress);
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            // Main chain create token
            await BootMinerChangeRoundAsync(true);
            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            var mainBlock = await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var createdTokenInfoBytes = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                });
            var createdTokenInfo = TokenInfo.Parser.ParseFrom(createdTokenInfoBytes);
            var tokenValidationTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.ValidateTokenInfoExists), null,
                new ValidateTokenInfoExistsInput
                {
                    TokenName = createdTokenInfo.TokenName,
                    Symbol = createdTokenInfo.Symbol,
                    Decimals = createdTokenInfo.Decimals,
                    Issuer = createdTokenInfo.Issuer,
                    IsTransferDisabled = createdTokenInfo.IsTransferDisabled,
                    IsBurnable = createdTokenInfo.IsBurnable,
                    TotalSupply = createdTokenInfo.TotalSupply,
                    IssueChainId = createdTokenInfo.IssueChainId
                }, true);
            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(mainBlock2.Height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = mainBlock.Height,
                TransactionBytes = tokenValidationTransaction.ToByteString(),
                MerklePath = merklePath
            };
            // Side chain cross chain create
            await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);


            //Main chain cross transfer to side chain
            await IssueTransactionAsync(SymbolForTesting, 1000);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);
            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var balanceResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput
                {
                    Owner = MainChainTester.GetCallOwnerAddress(),
                    Symbol = SymbolForTesting
                });
            var balance = GetBalanceOutput.Parser.ParseFrom(balanceResult);
            Assert.True(balance.Balance == 0);

            // can't issue Token on chain which is not issue chain
            var sideIssue = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Symbol = SymbolForTesting,
                    Amount = _totalSupply,
                    To = SideChainTester.GetCallOwnerAddress()
                });
            Assert.True(sideIssue.Status == TransactionResultStatus.Failed);
            Assert.Contains("Unable to issue token with wrong chainId", sideIssue.Error);
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_IncorrectChainId_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = sideChainId
                }, true);

            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Incorrect issue chain id.", txResult.Error);
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_InvalidChainId_Test()
        {
            var wrongSideChainId = ChainHelper.GetChainId(1);
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = wrongSideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);

            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid transfer target chain.", txResult.Error);
        }

        #endregion

        #region  cross chain receive 

        [Fact]
        public async Task SideChain_CrossChainReceived_NativeToken_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            //Main chain cross transfer to side chain
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);
            await BootMinerChangeRoundAsync(true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > ParentChainHeightOfCreation ? block.Height : ParentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(height, blockRoot);
            await BootMinerChangeRoundAsync(false);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = height,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
                MerklePath = transferMerKlePath
            };
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Mined);

            var balanceAfterTransfer = await SideChainTester.CallContractMethodAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = SideChainTester.GetCallOwnerAddress(),
                    Symbol = NativeToken
                });
            var balanceAfter = GetBalanceOutput.Parser.ParseFrom(balanceAfterTransfer).Balance;
            Assert.Equal(1000, balanceAfter);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_Twice_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            //Main chain cross transfer to side chain
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);
            await BootMinerChangeRoundAsync(true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > ParentChainHeightOfCreation ? block.Height : ParentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(height, blockRoot);
            await BootMinerChangeRoundAsync(false);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = height,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
                MerklePath = transferMerKlePath
            };
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Mined);

            var transferResultTwice = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResultTwice.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token already claimed.", transferResultTwice.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_InvalidToken_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);
            var transferAmount = 1000;

            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);
            await IssueTransactionAsync(SymbolForTesting, transferAmount);

            //Main chain cross transfer to side chain
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);
            await BootMinerChangeRoundAsync(true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > ParentChainHeightOfCreation ? block.Height : ParentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(height, blockRoot);
            await BootMinerChangeRoundAsync(false);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = height,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
                MerklePath = transferMerKlePath
            };
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);

            Assert.True(transferResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token is not found.", transferResult.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_WrongReceiver_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();
            await RegisterMainChainTokenContractAddressOnSideChainAsync(sideChainId);

            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = NativeToken,
                    ToChainId = sideChainId,
                    Amount = 1000,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, true);

            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(block.Height, blockRoot);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = block.Height,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
                MerklePath = transferMerKlePath
            };
            var otherUser = SideChainTester.CreateNewContractTester(SampleECKeyPairs.KeyPairs[2]);
            var transferResult = await otherUser.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Unable to claim cross chain token.", transferResult.Error);
        }

        #endregion


        #region private method

        private async Task<int> GenerateSideChainAsync()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId, 100);
            StartSideChain(sideChainId, ParentChainHeightOfCreation);
            return sideChainId;
        }

        private async Task<Transaction> ValidateTransactionAsync(Address basicContractZeroAddress,
            Address tokenContractAddress, Hash name, bool isMainChain)
        {
            var validateTransaction = await GenerateTransactionAsync(basicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ValidateSystemContractAddress), null,
                new ValidateSystemContractAddressInput
                {
                    Address = tokenContractAddress,
                    SystemContractHashName = name
                }, isMainChain);
            return validateTransaction;
        }

        private async Task<Transaction> CreateTransactionForTokenCreationAsync(Address tokenContractAddress,
            Address issuer, string symbol, bool isMainChain)
        {
            var tokenInfo = GetTokenInfo(symbol, issuer);
            var createTransaction = await GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Create), null,
                new CreateInput
                {
                    Symbol = tokenInfo.Symbol,
                    Decimals = tokenInfo.Decimals,
                    Issuer = tokenInfo.Issuer,
                    IsBurnable = tokenInfo.IsBurnable,
                    TokenName = tokenInfo.TokenName,
                    TotalSupply = tokenInfo.TotalSupply
                }, isMainChain);
            return createTransaction;
        }

        private async Task<Transaction> CreateTokenInfoValidationTransactionAsync(TokenInfo createdTokenInfo,
            Address tokenContractAddress, bool isMainChain)
        {
            var tokenValidationTransaction = await GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.ValidateTokenInfoExists), null,
                new ValidateTokenInfoExistsInput
                {
                    TokenName = createdTokenInfo.TokenName,
                    Symbol = createdTokenInfo.Symbol,
                    Decimals = createdTokenInfo.Decimals,
                    Issuer = createdTokenInfo.Issuer,
                    IsTransferDisabled = createdTokenInfo.IsTransferDisabled,
                    IsBurnable = createdTokenInfo.IsBurnable,
                    TotalSupply = createdTokenInfo.TotalSupply,
                    IssueChainId = createdTokenInfo.IssueChainId
                }, isMainChain);
            return tokenValidationTransaction;
        }

        private TokenInfo GetTokenInfo(string symbol, Address issuer)
        {
            return new TokenInfo
            {
                Symbol = symbol,
                Decimals = 2,
                Issuer = issuer,
                IsBurnable = true,
                TokenName = "Symbol for testing",
                TotalSupply = 1000
            };
        }

        private async Task IssueTransactionAsync(string symbol, long amount)
        {
            var txRes = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Issue),
                new IssueInput()
                {
                    Symbol = symbol,
                    To = MainChainTester.GetCallOwnerAddress(),
                    Amount = amount
                });
            Assert.True(txRes.Status == TransactionResultStatus.Mined);
            var balance = GetBalanceOutput.Parser.ParseFrom(MainChainTester.CallContractMethodAsync(
                TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = MainChainTester.GetCallOwnerAddress(),
                    Symbol = symbol
                }).Result).Balance;
            Assert.True(balance == amount);
        }

        private async Task RegisterMainChainTokenContractAddressOnSideChainAsync(int sideChainId)
        {
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, false);
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            //await BootMinerChangeRoundAsync(true);
            var result = await DoTokenContractRegistrationOnMainChainAsync(validateTransaction, merklePath,
                boundParentChainHeightAndMerklePath, sideChainId);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        private async Task RegisterSideChainContractAddressOnMainChainAsync()
        {
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            var height = block.Height > ParentChainHeightOfCreation ? block.Height : ParentChainHeightOfCreation;
            await IndexMainChainTransactionAsync(height, blockRoot);
            var result = await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath, height);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        private async Task<TransactionResult> DoTokenContractRegistrationOnSideChainAsync(Transaction transaction,
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
            var proposalId = await CreateProposalAsyncOnSideChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
            await ApproveWithMinersOnSideChainAsync(proposalId);
            return await ReleaseProposalAsync(proposalId, SideParliamentAddress, false);
        }

        private async Task<TransactionResult> DoTokenContractRegistrationOnMainChainAsync(Transaction transaction,
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
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            return await ReleaseProposalAsync(proposalId, ParliamentAddress, true);
        }

        private MerklePath GetTransactionMerklePathAndRoot(Transaction transaction, out Hash root)
        {
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = Hash.FromRawBytes(rawBytes);
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes((new[] {hash, fakeHash1, fakeHash2}));

            var merklePath = binaryMerkleTree.GenerateMerklePath(0);
            root = binaryMerkleTree.Root;
            return merklePath;
        }

        private async Task IndexMainChainTransactionAsync(long height, Hash root)
        {
            var indexParentHeight = await GetParentChainHeight();
            var crossChainBlockData = new CrossChainBlockData();
            var index = indexParentHeight >= ParentChainHeightOfCreation
                ? indexParentHeight + 1
                : ParentChainHeightOfCreation;
            for (var i = index; i < height; i++)
            {
                crossChainBlockData.ParentChainBlockDataList.Add(CreateParentChainBlockData(i, MainChainId,
                    root));
            }

            var parentChainBlockData = CreateParentChainBlockData(height, MainChainId, root);
            crossChainBlockData.ParentChainBlockDataList.Add(parentChainBlockData);

            parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
            {
                TransactionStatusMerkleTreeRoot = root
            };

            await BootMinerChangeRoundAsync(false);
            var proposingTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing), null,
                crossChainBlockData, false);
            await MineAsync(new List<Transaction> {proposingTx}, false);
            var proposingResult = await SideChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                false);
            await MineAsync(new List<Transaction> {releaseTx}, false);
            var result = await SideChainTester.GetTransactionResultAsync(releaseTx.GetHash());
            var releasedProposalId = ProposalReleased.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed).ProposalId;
            releasedProposalId.ShouldBe(proposalId);
            Assert.True(result.Status == TransactionResultStatus.Mined);

            var parentChainHeight = await GetParentChainHeight();
            parentChainHeight.ShouldBe(height);
        }

        private async Task DoIndexAsync(CrossChainBlockData crossChainBlockData, long sideTransactionHeight)
        {
            await BootMinerChangeRoundAsync(true);
            // proposing tx
            var proposingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing), null,
                crossChainBlockData, true);

            await MineAsync(new List<Transaction> {proposingTx}, true);
            var result = await MainChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;

            await ApproveWithMinersOnMainChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                true);

            var block2 = await MineAsync(new List<Transaction> {releaseTx}, true);

            await IndexMainChainBlockAsync(releaseTx, block2.Height, sideTransactionHeight,
                crossChainBlockData.SideChainBlockDataList.Select(b => b.TransactionStatusMerkleTreeRoot).ToList());
        }

        private async Task MainAndSideIndexAsync(int sideChainId, long sideTransactionHeight, Hash root)
        {
            //Main chain index side chain transaction
            var crossChainBlockData = new CrossChainBlockData();
            var mainChainIndexSideChain = await GetSideChainHeight(sideChainId);
            var height = mainChainIndexSideChain > 1 ? mainChainIndexSideChain + 1 : 1;
            for (var i = height; i < sideTransactionHeight; i++)
            {
                crossChainBlockData.SideChainBlockDataList.Add(CreateSideChainBlockData(FakeBlockHeader, i, sideChainId,
                    root));
            }

            var sideChainBlockData = CreateSideChainBlockData(FakeBlockHeader, sideTransactionHeight, sideChainId,
                root);
            crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);

            await DoIndexAsync(crossChainBlockData, sideTransactionHeight);
        }

        private async Task IndexMainChainBlockAsync(Transaction transaction, long height,
            long sideHeight,
            List<Hash> indexedSideChainBlockRoots)
        {
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = Hash.FromRawBytes(rawBytes);
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] {hash, fakeHash1, fakeHash2});
            var merkleTreeRootHash = binaryMerkleTree.Root;

            var indexParentHeight = await GetParentChainHeight();
            var nextHeightToBeIndexed = indexParentHeight >= ParentChainHeightOfCreation
                ? indexParentHeight + 1
                : ParentChainHeightOfCreation;
            var crossChainBlockData = new CrossChainBlockData();
            for (long i = nextHeightToBeIndexed; i < height; i++)
            {
                crossChainBlockData.ParentChainBlockDataList.Add(
                    CreateParentChainBlockData(i, MainChainId, Hash.FromTwoHashes(fakeHash1, fakeHash2)));
            }

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

            await BootMinerChangeRoundAsync(false);
            var proposingTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing), null,
                crossChainBlockData, false);
            await MineAsync(new List<Transaction> {proposingTx}, false);
            var proposingResult = await SideChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                false);
            await MineAsync(new List<Transaction> {releaseTx}, false);
            var result = await SideChainTester.GetTransactionResultAsync(releaseTx.GetHash());
            var releasedProposalId = ProposalReleased.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed).ProposalId;
            releasedProposalId.ShouldBe(proposalId);
            Assert.True(result.Status == TransactionResultStatus.Mined);
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

//        [Fact]
//        public void Test()
//        {
//            string txInString =
//                "CiIKIMsNuW2LTTcVKiJF8gvXSkXuaiLp5oS+PHFZocI6rwVTEiIKII08D3yDyP0Gn2SK++fkQ/X88sX9fcOuY5hKUWmK8PDnGJ+szwIiBOej8BUqEkNyb3NzQ2hhaW5UcmFuc2ZlcjI4CiIKII9p2olpPR6wjvgTSD2KAzeZGU0bg+4lxGOuA+ONEp2aEgNFTEYYgOKiyAEomPV5MJv04QSC8QRBOE6hml/+47ajPF8xWCrad/GHwmQHdrqOK3jMFzwjJskW7jxauEb5twyl50LeQrMKmw8zQQOCF46wjT8lNLDhPgE=";
//            var tx = Transaction.Parser.ParseFrom(ByteString.FromBase64(txInString));
//            Assert.True(tx.VerifySignature());
//
//            var param = CrossChainTransferInput.Parser.ParseFrom(tx.Params);
//            ;
//
//            var tx2 = new Transaction
//            {
//                From = Address.FromBytes(
//                    Base58CheckEncoding.Decode("21Mr89oryCdKjcaGgQtPCiCmCqvCcV2BuPLyPk3j1GduBfKwga")),
//                To = Address.FromBytes(
//                    Base58CheckEncoding.Decode("25CecrU94dmMdbhC3LWMKxtoaL4Wv8PChGvVJM6PxkHAyvXEhB")),
//                RefBlockNumber = 5494275,
//                RefBlockPrefix = ByteString.FromBase64("OMKzaw=="),
//                MethodName = "Transfer",
//                Params = new TransferInput()
//                {
//                    To = Address.FromBytes(
//                        Base58CheckEncoding.Decode("P485ma9uqY7hi8rZixRPqETdgwuyaFFUQNxn7q8pXzjKNASij")),
//                    Amount = 2000,
////                    ToChainId = 1997464,
////                    IssueChainId = 9992731,
//                    Symbol = "ELF",
//                    Memo = "transfer test - dc9258b5-6e23-4626-9278-cd8991802626"
//                }.ToByteString(),
//                Signature = ByteString.FromBase64(
//                    "sfl48wQquZkyb5rq8y3ArooFGkfRWSVdnRWVhYYYA2Vuola4PCgpOhAnLnkLaVEO24VdygIf7BN88zePT9Cv3AE=")
//            };
////            Assert.Equal(tx, tx2);
////            Assert.True(tx2.VerifySignature());
//
//            Assert.Equal(tx.GetHash(), tx2.GetHash());
//            Assert.Equal(txInString, tx2.ToByteString().ToBase64());
//            var privateKey = "2eb0e2493fcd2bcc997a381c8f8a5ee6e7b2d51cddee4913d382333ac33482e5";
////            var keyPair = CryptoHelper.FromPrivateKey(privateKey.ToByteString().ToByteArray());
//            var signature = CryptoHelper.SignWithPrivateKey(privateKey.ToByteString().ToByteArray(), tx2.GetHash().ToByteArray());
//            ;
//        }
    }
}