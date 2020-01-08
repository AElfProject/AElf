using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.TestBase;
using AElf.CrossChain;
using AElf.CSharp.Core.Utils;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using SampleECKeyPairs = AElf.Contracts.TestKit.SampleECKeyPairs;
using ProposalCreated = Acs3.ProposalCreated;
using ProposalReleased = Acs3.ProposalReleased;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractCrossChainTest : MultiTokenContractCrossChainTestBase
    {
        private const string SymbolForTesting = "ELFTEST";
        private const string NativeToken = "ELF";
        private static long _totalSupply = 1000L;
        private readonly int _parentChainHeightOfCreation = 9;
        private readonly Hash _fakeBlockHeader = Hash.FromString("fakeBlockHeader");
        private string sideChainSymbol = "STA";

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
            var result = await RegisterSideChainTokenContractAsync(MainChainTester, ParliamentAddress,
                TokenContractAddress, validateTransaction, merklePath,
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
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, _parentChainHeightOfCreation, blockRoot, blockRoot);
            //Side chain register
            var result =
                await RegisterMainChainTokenContractOnSideChainAsync(validateTransaction, merklePath,
                    _parentChainHeightOfCreation);
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
                    ParentChainHeight = _parentChainHeightOfCreation,
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
            var result = await RegisterSideChainTokenContractAsync(MainChainTester, ParliamentAddress,
                TokenContractAddress, validateTransaction, merklePath,
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

            var result = await RegisterSideChainTokenContractAsync(MainChainTester, ParliamentAddress,
                TokenContractAddress, validateTransaction, merklePath,
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
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, _parentChainHeightOfCreation, blockRoot, blockRoot);
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
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, SideTokenContractAddress,
                    SideChainTester);

            var sideBlock2 = await SideChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
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
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
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
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, TokenContractAddress,
                    MainChainTester);

            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, mainBlock2.Height, blockRoot, blockRoot);
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
        public async Task SideChain_CrossChainSideChainCreateToken_Test()
        {
            var sideChainId1 = await GenerateSideChainAsync();
            await GenerateSideChain2Async();
            await RegisterSideChainContractAddressOnSideChainAsync(sideChainId1);

            // side chain 1 valid token
            await BootMinerChangeRoundAsync(SideChainTester, SideConsensusAddress, false);
            var createdTokenInfoBytes = await SideChainTester.CallContractMethodAsync(SideTokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = sideChainSymbol
                });
            var createdTokenInfo = TokenInfo.Parser.ParseFrom(createdTokenInfoBytes);
            var tokenValidationTransaction =
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, SideTokenContractAddress,
                    SideChainTester);

            var sideBlock2 = await SideChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await SideIndexSideChainAsync(sideChainId1, sideBlock2.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(sideBlock2.Height);

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
            var result = await SideChain2Tester.ExecuteContractWithMiningAsync(Side2TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainCreateToken), crossChainCreateTokenInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var newTokenInfo = TokenInfo.Parser.ParseFrom(await SideChain2Tester.CallContractMethodAsync(
                Side2TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = sideChainSymbol
                }));
            Assert.True(newTokenInfo.TotalSupply == createdTokenInfo.TotalSupply);
            Assert.True(newTokenInfo.Issuer == createdTokenInfo.Issuer);
            Assert.True(newTokenInfo.IssueChainId == createdTokenInfo.IssueChainId);
            Assert.True(newTokenInfo.Symbol == createdTokenInfo.Symbol);
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
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, _parentChainHeightOfCreation, blockRoot, blockRoot);
            var crossChainCreateTokenInput = new CrossChainCreateTokenInput
            {
                FromChainId = MainChainId,
                ParentChainHeight = _parentChainHeightOfCreation,
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

            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
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
                await CreateTokenInfoValidationTransactionAsync(createdTokenInfo, TokenContractAddress,
                    MainChainTester);
            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, mainBlock2.Height, blockRoot, blockRoot);
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
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
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
                    IsBurnable = createdTokenInfo.IsBurnable,
                    TotalSupply = createdTokenInfo.TotalSupply,
                    IssueChainId = createdTokenInfo.IssueChainId
                }, true);
            var mainBlock2 = await MainChainTester.MineAsync(new List<Transaction> {tokenValidationTransaction});
            var merklePath = GetTransactionMerklePathAndRoot(tokenValidationTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, mainBlock2.Height, blockRoot, blockRoot);
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
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > _parentChainHeightOfCreation ? block.Height : _parentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, height, blockRoot, blockRoot);
            await BootMinerChangeRoundAsync(SideChainTester, SideConsensusAddress, false);
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
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > _parentChainHeightOfCreation ? block.Height : _parentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, height, blockRoot, blockRoot);
            await BootMinerChangeRoundAsync(SideChainTester, SideConsensusAddress, false);
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
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
            var block = await MainChainTester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await MainChainTester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var height = block.Height > _parentChainHeightOfCreation ? block.Height : _parentChainHeightOfCreation;
            var transferMerKlePath = GetTransactionMerklePathAndRoot(crossChainTransferTransaction, out var blockRoot);
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, height, blockRoot, blockRoot);
            await BootMinerChangeRoundAsync(SideChainTester, SideConsensusAddress, false);
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
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, block.Height, blockRoot, blockRoot);
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
            var sideChainId =
                await InitAndCreateSideChainAsync(sideChainSymbol, _parentChainHeightOfCreation, MainChainId, 100);
            StartSideChain(sideChainId, _parentChainHeightOfCreation, sideChainSymbol);
            return sideChainId;
        }

        private async Task GenerateSideChain2Async()
        {
            var sideChainId = await InitAndCreateSideChainAsync("STB", _parentChainHeightOfCreation, MainChainId, 100);
            StartSideChain2(sideChainId, _parentChainHeightOfCreation, "STB");
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
            Address tokenContractAddress, ContractTester<MultiTokenContractCrossChainTestAElfModule> tester)
        {
            var tokenValidationTransaction = await tester.GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.ValidateTokenInfoExists),
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
            var result = await RegisterSideChainTokenContractAsync(MainChainTester, ParliamentAddress,
                TokenContractAddress, validateTransaction, merklePath,
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
            var height = block.Height > _parentChainHeightOfCreation ? block.Height : _parentChainHeightOfCreation;
            await IndexMainChainTransactionAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, height, blockRoot, blockRoot);
            var result = await RegisterMainChainTokenContractOnSideChainAsync(validateTransaction, merklePath, height);
            Assert.True(result.Status == TransactionResultStatus.Mined);
        }

        private async Task RegisterSideChainContractAddressOnSideChainAsync(int sideChainId)
        {
            var validateTransaction1 = await ValidateTransactionAsync(SideBasicContractZeroAddress,
                SideTokenContractAddress,
                TokenSmartContractAddressNameProvider.Name, false);
            var side1Block = await SideChainTester.MineAsync(new List<Transaction> {validateTransaction1});
            var validateResult = await SideChainTester.GetTransactionResultAsync(validateTransaction1.GetHash());
            Assert.True(validateResult.Status == TransactionResultStatus.Mined);

            var merklePath = GetTransactionMerklePathAndRoot(validateTransaction1, out var blockRoot);
            await SideIndexSideChainAsync(sideChainId, side1Block.Height, blockRoot);
            var boundParentChainHeightAndMerklePath =
                await GetBoundParentChainHeightAndMerklePathByHeight(side1Block.Height);

            var result = await RegisterSideChainTokenContractAsync(SideChain2Tester, Side2ParliamentAddress,
                Side2TokenContractAddress, validateTransaction1, merklePath,
                boundParentChainHeightAndMerklePath, sideChainId);
            Assert.True(result.Status == TransactionResultStatus.Mined);
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
            var proposalId = await CreateProposalAsync(SideChainTester, SideParliamentAddress,
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), SideTokenContractAddress);
            await ApproveWithMinersAsync(proposalId, SideParliamentAddress, SideChainTester);
            return await ReleaseProposalAsync(proposalId, SideParliamentAddress, SideChainTester);
        }

        private async Task<TransactionResult> RegisterSideChainTokenContractAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester, Address parliamentContract,
            Address tokenContract, IMessage transaction,
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
            var proposalId = await CreateProposalAsync(tester, parliamentContract,
                nameof(TokenContractContainer.TokenContractStub.RegisterCrossChainTokenContractAddress),
                registerCrossChainTokenContractAddressInput.ToByteString(), tokenContract);
            await ApproveWithMinersAsync(proposalId, parliamentContract, tester);
            return await ReleaseProposalAsync(proposalId, parliamentContract, tester);
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

        private async Task IndexMainChainTransactionAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester, Address consensusContract,
            Address crossChainContract, Address parliamentContract, long height, Hash txRoot, Hash blockRoot)
        {
            var indexParentHeight = await GetParentChainHeight(tester, crossChainContract);
            var crossChainBlockData = new CrossChainBlockData();
            var index = indexParentHeight >= _parentChainHeightOfCreation
                ? indexParentHeight + 1
                : _parentChainHeightOfCreation;
            for (var i = index; i < height; i++)
            {
                crossChainBlockData.ParentChainBlockDataList.Add(CreateParentChainBlockData(i, MainChainId,
                    txRoot));
            }

            var parentChainBlockData = CreateParentChainBlockData(height, MainChainId, txRoot);
            crossChainBlockData.ParentChainBlockDataList.Add(parentChainBlockData);

            parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
            {
                TransactionStatusMerkleTreeRoot = blockRoot
            };

            await BootMinerChangeRoundAsync(tester, consensusContract, false);
            var proposingTx = await tester.GenerateTransactionAsync(crossChainContract,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
                crossChainBlockData);
            await tester.MineAsync(new List<Transaction> {proposingTx});
            var proposingResult = await tester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersAsync(proposalId, parliamentContract, tester);
            var releaseTx = await tester.GenerateTransactionAsync(crossChainContract,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), proposalId);
            await tester.MineAsync(new List<Transaction> {releaseTx});
            var result = await tester.GetTransactionResultAsync(releaseTx.GetHash());
            var releasedProposalId = ProposalReleased.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed).ProposalId;
            releasedProposalId.ShouldBe(proposalId);
            Assert.True(result.Status == TransactionResultStatus.Mined);

            var parentChainHeight = await GetParentChainHeight(tester, crossChainContract);
            parentChainHeight.ShouldBe(height);
        }

        private async Task DoIndexAsync(CrossChainBlockData crossChainBlockData, long sideTransactionHeight)
        {
            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
            // proposing tx
            var proposingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing), null,
                crossChainBlockData, true);

            await MainChainTester.MineAsync(new List<Transaction> {proposingTx});
            var result = await MainChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;

            await ApproveWithMinersAsync(proposalId, ParliamentAddress, MainChainTester);
            var releaseTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), null, proposalId,
                true);

            var block2 = await MainChainTester.MineAsync(new List<Transaction> {releaseTx});

            await IndexMainChainBlockAsync(SideChainTester, SideConsensusAddress, SideCrossChainContractAddress,
                SideParliamentAddress, releaseTx, block2.Height, sideTransactionHeight,
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
                crossChainBlockData.SideChainBlockDataList.Add(CreateSideChainBlockData(_fakeBlockHeader, i, sideChainId,
                    root));
            }

            var sideChainBlockData = CreateSideChainBlockData(_fakeBlockHeader, sideTransactionHeight, sideChainId,
                root);
            crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);

            await DoIndexAsync(crossChainBlockData, sideTransactionHeight);
        }

        private async Task SideIndexSideChainAsync(int sideChainId, long sideTransactionHeight, Hash root)
        {
            //Main chain index side chain transaction
            var crossChainBlockData = new CrossChainBlockData();
            var mainChainIndexSideChain = await GetSideChainHeight(sideChainId);
            var height = mainChainIndexSideChain > 1 ? mainChainIndexSideChain + 1 : 1;
            for (var i = height; i < sideTransactionHeight; i++)
            {
                crossChainBlockData.SideChainBlockDataList.Add(CreateSideChainBlockData(_fakeBlockHeader, i, sideChainId,
                    root));
            }

            var sideChainBlockData = CreateSideChainBlockData(_fakeBlockHeader, sideTransactionHeight, sideChainId,
                root);
            crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);

            await BootMinerChangeRoundAsync(MainChainTester, ConsensusAddress, true);
            // proposing tx
            var proposingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing), null,
                crossChainBlockData, true);

            await MainChainTester.MineAsync(new List<Transaction> {proposingTx});
            var result = await MainChainTester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;

            await ApproveWithMinersAsync(proposalId, ParliamentAddress, MainChainTester);
            var releaseTx = await MainChainTester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), proposalId);
            var block2 = await MainChainTester.MineAsync(new List<Transaction> {releaseTx});

            var blockRoot = await IndexMainChainBlockAsync(SideChainTester, SideConsensusAddress,
                SideCrossChainContractAddress,
                SideParliamentAddress, releaseTx, block2.Height, sideTransactionHeight,
                crossChainBlockData.SideChainBlockDataList.Select(b => b.TransactionStatusMerkleTreeRoot).ToList());

            GetTransactionMerklePathAndRoot(releaseTx, out var txRoot);
            await IndexMainChainTransactionAsync(SideChain2Tester, Side2ConsensusAddress,
                Side2CrossChainContractAddress, Side2ParliamentAddress, block2.Height, txRoot, blockRoot);
        }

        private async Task<Hash> IndexMainChainBlockAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester,
            Address consensusContract, Address crossChainContract, Address parliamentContract, Transaction transaction,
            long height,
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

            var indexParentHeight = await GetParentChainHeight(tester, crossChainContract);
            var nextHeightToBeIndexed = indexParentHeight >= _parentChainHeightOfCreation
                ? indexParentHeight + 1
                : _parentChainHeightOfCreation;
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

            await BootMinerChangeRoundAsync(tester, consensusContract, false);
            var proposingTx = await tester.GenerateTransactionAsync(crossChainContract,
                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
                crossChainBlockData);
            await tester.MineAsync(new List<Transaction> {proposingTx});
            var proposingResult = await tester.GetTransactionResultAsync(proposingTx.GetHash());
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersAsync(proposalId, parliamentContract, tester);
            var releaseTx = await tester.GenerateTransactionAsync(crossChainContract,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing), proposalId);
            await tester.MineAsync(new List<Transaction> {releaseTx});
            var result = await tester.GetTransactionResultAsync(releaseTx.GetHash());
            var releasedProposalId = ProposalReleased.Parser
                .ParseFrom(result.Logs.First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed).ProposalId;
            releasedProposalId.ShouldBe(proposalId);
            Assert.True(result.Status == TransactionResultStatus.Mined);

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
}