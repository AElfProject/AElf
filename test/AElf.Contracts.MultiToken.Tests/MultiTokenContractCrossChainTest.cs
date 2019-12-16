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
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
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
                TokenSmartContractAddressNameProvider.Name, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            // Index main chain
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(ParentChainHeightOfCreation, blockRoot);
            //Side chain register
            var result = await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath, ParentChainHeightOfCreation);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_WithoutPermission_Test()
        {
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, "Main");
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
                SideCrossChainContractAddress, CrossChainSmartContractAddressNameProvider.Name, "Side");
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
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
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
                TokenSmartContractAddressNameProvider.Name, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            // Index main chain
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(ParentChainHeightOfCreation, blockRoot);
            // Wrong merklePath
            merklePath.MerklePathNodes.AddRange(merklePath.MerklePathNodes);
            //Side chain register
            var result = await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath, ParentChainHeightOfCreation);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Cross chain verification failed.", result.Error);
        }

        #endregion

        #region cross chain create token test

        [Fact]
        public async Task MainChain_CrossChainCreateToken_Failed_Test()
        {
            var sideChainId = await GenerateSideChainAsync();
            // Side chain create token
            var createTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await SideChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = boundParentChainHeightAndMerklePath.BoundParentChainHeight,
                TransactionBytes = createTransaction.ToByteString(),
                MerklePath = merklePath
            };
            crossChainCreateTokenInput.MerklePath.MerklePathNodes.AddRange(boundParentChainHeightAndMerklePath
                .MerklePathFromParentChain.MerklePathNodes);
            // Main chain cross chain create
            var result = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Assertion failed!", result.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_Test()
        {
            await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();

            // Main chain create token
            await BootMinerChangeRoundAsync(true);
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            var mainBlock = await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var height = mainBlock.Height > ParentChainHeightOfCreation
                ? mainBlock.Height
                : ParentChainHeightOfCreation;
            var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = height,
                TransactionBytes = createTransaction.ToByteString(),
                MerklePath = merklePath
            };
            // Side chain cross chain create
            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Mined);

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
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
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
            Assert.Contains("Token contract address of parent chain not found.", result.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_WithAlreadyCreated_Test()
        {
            await GenerateSideChainAsync();
            await RegisterSideChainContractAddressOnMainChainAsync();

            await BootMinerChangeRoundAsync(true);
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            var mainBlock = await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var sideCreateTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {sideCreateTransaction});
            var sideCreateResult = await SideChainTester.GetTransactionResultAsync(sideCreateTransaction.GetHash());
            Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined);

            var height = mainBlock.Height > ParentChainHeightOfCreation
                ? mainBlock.Height
                : ParentChainHeightOfCreation;
            var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = height,
                TransactionBytes = createTransaction.ToByteString(),
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
                }, "Main");

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
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            var mainBlock = await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(createTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(mainBlock.Height, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = mainBlock.Height,
                TransactionBytes = createTransaction.ToByteString(),
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
                }, "Main");
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
                }, "Main");

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
                }, "Main");

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
                }, "Main");
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
                }, "Main");
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

            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
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
                }, "Main");
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
                }, "Main");

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
            Address tokenContractAddress, Hash name, string chainType)
        {
            var validateTransaction = await GenerateTransactionAsync(basicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ValidateSystemContractAddress), null,
                new ValidateSystemContractAddressInput
                {
                    Address = tokenContractAddress,
                    SystemContractHashName = name
                }, chainType);
            return validateTransaction;
        }

        private async Task<Transaction> CreateTransactionAsync(Address tokenContractAddress, Address issuer,
            string symbol, string chainType)
        {
            var createTransaction = await GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Create), null,
                new CreateInput
                {
                    Symbol = symbol,
                    Decimals = 2,
                    Issuer = issuer,
                    IsBurnable = true,
                    TokenName = "Symbol for testing",
                    TotalSupply = 1000
                }, chainType);
            return createTransaction;
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
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            var sideBlock = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            await MainAndSideIndexAsync(sideChainId, sideBlock.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock.Height);

            await BootMinerChangeRoundAsync(true);
            var result = await DoTokenContractRegistrationOnMainChainAsync(validateTransaction, merklePath,
                boundParentChainHeightAndMerklePath, sideChainId);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        private async Task RegisterSideChainContractAddressOnMainChainAsync()
        {
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, "Main");
            var block = await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);
            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction, out var blockRoot);
            var height = block.Height > ParentChainHeightOfCreation ? block.Height : ParentChainHeightOfCreation;
            await IndexMainChainTransactionAsync(height, blockRoot);
            var result = await DoTokenContractRegistrationOnSideChainAsync(validateTransaction, merklePath, height);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        private async Task<TransactionResult> DoTokenContractRegistrationOnSideChainAsync(Transaction transaction, MerklePath merklePath,
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
            return await ReleaseProposalAsync(proposalId, SideParliamentAddress, "Side");
        }

        private async Task<TransactionResult> DoTokenContractRegistrationOnMainChainAsync(Transaction transaction, MerklePath merklePath,
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
            return await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
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
                crossChainBlockData, "Side");
            await MineAsync(new List<Transaction> {proposingTx}, "Side");
            var proposingResult = await SideChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                "Side");
            await MineAsync(new List<Transaction> {releaseTx}, "Side");
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
                crossChainBlockData, "Main");

            await MineAsync(new List<Transaction> {proposingTx}, "Main");
            var result = await MainChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;

            await ApproveWithMinersOnMainChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                "Main");

            var block2 = await MineAsync(new List<Transaction> {releaseTx}, "Main");

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
                crossChainBlockData, "Side");
            await MineAsync(new List<Transaction> {proposingTx}, "Side");
            var proposingResult = await SideChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var releaseTx = await GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                "Side");
            await MineAsync(new List<Transaction> {releaseTx}, "Side");
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
    }
}