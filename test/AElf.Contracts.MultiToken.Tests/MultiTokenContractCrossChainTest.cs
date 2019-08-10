using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs7;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Xunit;
using SampleAddress = AElf.Contracts.TestKit.SampleAddress;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractCrossChainTest : MultiTokenContractCrossChainTestBase
    {
        private const string SymbolForTesting = "ELFTEST";
        private const string NativeToken = "ELF";
        private static long _totalSupply = 1000L;
        private int ParentChainHeightOfCreation = 10;

        [Fact]
        public async Task MainChain_RegisterCrossChainTokenContractAddress_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);
            //Side chain validate transaction
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            // Main chain register
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString(),
                merklePath.Path);
            };
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task SideChain_RegisterCrossChainTokenContractAddress_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);
            await InitializeCrossChainContractOnSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            //Main chain validate transaction
            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexMainChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            //Side chain register
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = TokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnSideChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, SideParliamentAddress, "Side");
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
        public async Task RegisterCrossChainTokenContractAddress_AddressValidationFailed_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                CrossChainContractAddress, CrossChainSmartContractAddressNameProvider.Name, "Side");
            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnSideChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, SideParliamentAddress, "Side");
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Address validation failed.", result.Error);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_InvalidTransaction_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);

            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.Contains("Invalid transaction", result.Error);
        }

        [Fact]
        public async Task RegisterCrossChainTokenContractAddress_VerificationFailed_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);
            await InitializeCrossChainContractOnSideChainAsync(ParentChainHeightOfCreation, MainChainId);

            var validateTransaction = await ValidateTransactionAsync(BasicContractZeroAddress, TokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await MainChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var merklePath = await IndexMainChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation,
                MainChainId, fakeTransactionStatusMerkleRoot);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = TokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnSideChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
            await ApproveWithMinersOnSideChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, SideParliamentAddress, "Side");
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Cross chain verification failed.", result.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_Test()
        {
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            // Main chain create token
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexMainChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);
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
        public async Task MainChain_CrossChainCreateToken_Failed_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            await MainChain_RegisterCrossChainTokenContractAddress_Test();
            // Side chain create token
            var createTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await SideChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);
            // Main chain cross chain create
            var result = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token contract address of parent chain not found.", result.Error);
        }

        [Fact]
        public async Task CrossChainCreateToken_WithoutRegister_Test()
        {
            var sideChainId = await InitAndCreateSideChainAsync(ParentChainHeightOfCreation, MainChainId);
            StartSideChain(sideChainId);
            await InitializeCrossChainContractOnSideChainAsync(ParentChainHeightOfCreation, MainChainId);

            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexMainChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token contract address of parent chain not found.", result.Error);
        }

        [Fact]
        public async Task CrossChainCreateToken_InvalidTransaction_Test()
        {
            await SideChain_RegisterCrossChainTokenContractAddress_Test();

            var createTransaction = await OtherTransactionAsync(TokenContractAddress, "ELF", "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexMainChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid transaction", result.Error);
        }

        [Fact]
        public async Task CrossChainCreateToken_VerificationFailed_Test()
        {
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var fakeMerkleTreeRoot = Hash.FromString("FakeMerkleTreeRoot");
            var merklePath =
                await IndexMainChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId,
                    fakeMerkleTreeRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Cross chain verification failed.", result.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainCreateToken_WithAlreadyCreated_Test()
        {
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var sideCreateTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {sideCreateTransaction});
            var sideCreateResult = await SideChainTester.GetTransactionResultAsync(sideCreateTransaction.GetHash());
            Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexMainChainTransactionAsync(createTransaction, ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransactionBytes = createTransaction.ToByteString()
            };
            crossChainCreateTokenInput.MerklePath.AddRange(merklePath.Path);

            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token already exists.", result.Error);
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_NativeToken_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            // side chain register and cross chain create token
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);
            
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
        }
        
        [Fact]
        public async Task MainChain_CrossChainTransfer_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            // side chain register and cross chain create token
            await SideChain_CrossChainCreateToken_Test();
            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

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
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_IncorrectChainId_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            await SideChain_CrossChainCreateToken_Test();
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

            await IssueTransactionAsync(SymbolForTesting, 1000);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
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
            var sideChainId = ChainHelper.GetChainId(1);
            await SideChain_CrossChainCreateToken_Test();
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
            Assert.True(txResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid transfer target chain.", txResult.Error);
        }

        [Fact]
        public async Task MainChain_CrossChainTransfer_WithoutCrossChainCreate_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var sideCreateTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {sideCreateTransaction});
            var sideCreateResult = await SideChainTester.GetTransactionResultAsync(sideCreateTransaction.GetHash());
            Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined);

            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);
            
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
        }
        
        [Fact]
        public async Task SideChain_CrossChainReceived_NativeToken_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            // side chain register 
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

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

            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 1, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 1,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
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
        public async Task SideChain_CrossChainReceived_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var transferAmount = 900;
            // side chain register and cross chain create token
            await SideChain_CrossChainCreateToken_Test();
            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var validateMerklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(validateMerklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

            //Main chain cross transfer to side chain
            await IssueTransactionAsync(SymbolForTesting, _totalSupply);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
                    ToChainId = sideChainId,
                    Amount = transferAmount,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            //Side chain receive token
            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 2, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 2,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Mined);
            var balanceAfterTransfer = await SideChainTester.CallContractMethodAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = SideChainTester.GetCallOwnerAddress(),
                    Symbol = SymbolForTesting
                });
            var balanceAfter = GetBalanceOutput.Parser.ParseFrom(balanceAfterTransfer).Balance;
            Assert.Equal(transferAmount, balanceAfter);

            var mainSupply = TokenInfo.Parser.ParseFrom(await MainChainTester.CallContractMethodAsync(
                TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                })).Supply;
            Assert.True(mainSupply == (_totalSupply - balanceAfter));

            var sideSupply = TokenInfo.Parser.ParseFrom(await SideChainTester.CallContractMethodAsync(
                SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = SymbolForTesting
                })).Supply;
            Assert.True(sideSupply == balanceAfter);

            // can't issue Token on chain which is not issue chain
            var sideIssue = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Symbol = SymbolForTesting,
                    Amount = _totalSupply,
                    To = SideChainTester.GetCallOwnerAddress()
                });
            Assert.True(sideIssue.Status == TransactionResultStatus.Failed);
            Assert.Contains("Total supply exceeded", sideIssue.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_Twice_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var transferAmount = 1000;
            await SideChain_CrossChainCreateToken_Test();

            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var validateMerklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(validateMerklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

            await IssueTransactionAsync(SymbolForTesting, transferAmount);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
                    ToChainId = sideChainId,
                    Amount = transferAmount,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 2, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 2,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Mined);

            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResultTwice = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResultTwice.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token already claimed.", transferResultTwice.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_InvalidToken_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var transferAmount = 1000;
            var otherSymbol = "TEST";
            await SideChain_CrossChainCreateToken_Test();
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var validateMerklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(validateMerklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

            // create other token
            var createOtherToken = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), otherSymbol, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createOtherToken});
            await IssueTransactionAsync(otherSymbol, transferAmount);

            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = otherSymbol,
                    ToChainId = sideChainId,
                    Amount = transferAmount,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 2, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 2,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token is not found.", transferResult.Error);
        }
        
        [Fact]
        public async Task SideChain_CrossChainReceive_WithoutCrossChainCreate_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            await SideChain_RegisterCrossChainTokenContractAddress_Test();
            var createTransaction = await CreateTransactionAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined);

            var sideCreateTransaction = await CreateTransactionAsync(SideTokenContractAddress,
                SideChainTester.GetCallOwnerAddress(), SymbolForTesting, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {sideCreateTransaction});
            var sideCreateResult = await SideChainTester.GetTransactionResultAsync(sideCreateTransaction.GetHash());
            Assert.True(sideCreateResult.Status == TransactionResultStatus.Mined);

            // main chain register 
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(merklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);
            
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
            
            //Side chain receive token
            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 2, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 2,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResult = await SideChainTester.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Incorrect issue chain id.", transferResult.Error);
        }

        [Fact]
        public async Task SideChain_CrossChainReceived_WrongReceiver_Test()
        {
            var sideChainId = ChainHelper.GetChainId(1);
            var transferAmount = 1000;
            await SideChain_CrossChainCreateToken_Test();
            var validateTransaction = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress, TokenSmartContractAddressNameProvider.Name, "Side");
            await SideChainTester.MineAsync(new List<Transaction> {validateTransaction});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var validateMerklePath =
                await IndexSideChainTransactionAsync(validateTransaction, ParentChainHeightOfCreation, MainChainId);
            var registerCrossChainTokenContractAddressInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = sideChainId,
                ParentChainHeight = ParentChainHeightOfCreation,
                TokenContractAddress = SideTokenContractAddress,
                TransactionBytes = validateTransaction.ToByteString()
            };
            registerCrossChainTokenContractAddressInput.MerklePath.AddRange(validateMerklePath.Path);
            var proposalId = await CreateProposalAsyncOnMainChain(
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), TokenContractAddress);
            await ApproveWithMinersOnMainChainAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, ParliamentAddress, "Main");
            Assert.True(result.Status == TransactionResultStatus.Mined);

            await IssueTransactionAsync(SymbolForTesting, transferAmount);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    Symbol = SymbolForTesting,
                    ToChainId = sideChainId,
                    Amount = transferAmount,
                    To = SideChainTester.GetCallOwnerAddress(),
                    IssueChainId = MainChainId
                }, "Main");
            await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var transferMerKlePath = await IndexMainChainTransactionAsync(crossChainTransferTransaction,
                ParentChainHeightOfCreation + 2, MainChainId);
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = ParentChainHeightOfCreation + 2,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString()
            };
            var otherUser = SideChainTester.CreateNewContractTester(SampleECKeyPairs.KeyPairs[2]);
            crossChainReceiveTokenInput.MerklePath.AddRange(transferMerKlePath.Path);
            var transferResult = await otherUser.ExecuteContractWithMiningAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(transferResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Unable to claim cross chain token.", transferResult.Error);
        }

        #region private method

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

        private async Task<Transaction> OtherTransactionAsync(Address tokenContractAddress, string symbol,
            string chainType)
        {
            var createTransaction = await GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Transfer), null,
                new TransferInput()
                {
                    Symbol = symbol,
                    Amount = 1,
                    To = SampleAddress.AddressList[1]
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

        private async Task<MerklePath> IndexSideChainTransactionAsync(Transaction transaction, long height, int chainId,
            Hash fakeMerkleTreeRoot = null)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = Hash.FromRawBytes(rawBytes);
            binaryMerkleTree.AddNodes(new[] {hash, fakeHash1, fakeHash2});

            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(binaryMerkleTree.GenerateMerklePath(0));
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");

            var parentChainBlockData = CreateParentChainBlockData(height, chainId,
                fakeTransactionStatusMerkleRoot);
            parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
            {
                SideChainTransactionsRoot = fakeMerkleTreeRoot == null ? merkleTreeRoot : fakeMerkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData, "Main");
            await MineAsync(new List<Transaction> {indexingTx}, "Main");

            return merklePath;
        }

        private async Task<MerklePath> IndexMainChainTransactionAsync(Transaction transaction, long height, int chainId,
            Hash fakeMerkleTreeRoot = null)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = Hash.FromRawBytes(rawBytes);
            binaryMerkleTree.AddNodes(new[] {hash, fakeHash1, fakeHash2});
            var merkleTreeRootHash = binaryMerkleTree.ComputeRootHash();
            var merkleTreeRoot = fakeMerkleTreeRoot == null ? merkleTreeRootHash : fakeMerkleTreeRoot;
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(binaryMerkleTree.GenerateMerklePath(0));
            var parentChainBlockData = CreateParentChainBlockData(height, chainId,
                merkleTreeRoot);

            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var indexingTx = await SideChainTester.GenerateTransactionAsync(SideCrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);
            await SideChainTester.MineAsync(new List<Transaction> {indexingTx});

            return merklePath;
        }

        private ParentChainBlockData CreateParentChainBlockData(long height, int sideChainId, Hash txMerkleTreeRoot)
        {
            return new ParentChainBlockData
            {
                ChainId = sideChainId,
                Height = height,
                TransactionStatusMerkleRoot = txMerkleTreeRoot
            };
        }

        #endregion
    }
}