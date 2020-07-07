using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.TestBase;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using ProposalCreated = Acs3.ProposalCreated;
using ProposalReleased = Acs3.ProposalReleased;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractCrossChainWithoutRegisterTest : MultiTokenContractCrossChainTestBase
    {
        private const string SymbolForTesting = "ELFTEST";
        private static long _totalSupply = 1000L;
        private readonly int _parentChainHeightOfCreation = 9;
        private string sideChainSymbol = "STA";
        
        #region cross chain create token test

        [Fact]
        public async Task CrossChainCreateToken_WithoutRegister_Test()
        {
            await GenerateSideChainAsync(false);
            var createTransaction = await CreateTransactionForTokenCreationAsync(TokenContractAddress,
                MainChainTester.GetCallOwnerAddress(), SymbolForTesting, true);
            await MainChainTester.MineAsync(new List<Transaction> {createTransaction});
            var createResult = await MainChainTester.GetTransactionResultAsync(createTransaction.GetHash());
            Assert.True(createResult.Status == TransactionResultStatus.Mined, createResult.Error);

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
                nameof(TokenContractImplContainer.TokenContractImplStub.CrossChainCreateToken),
                crossChainCreateTokenInput);
            Assert.True(result.Status == TransactionResultStatus.Failed);
            Assert.Contains("Token contract address of chain AELF not registered.", result.Error);
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

        private async Task<Transaction> CreateTransactionForTokenCreationAsync(Address tokenContractAddress,
            Address issuer, string symbol, bool isMainChain, bool isBurnable = true)
        {
            var tokenInfo = GetTokenInfo(symbol, issuer, isBurnable);
            var createTransaction = await GenerateTransactionAsync(tokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.Create), null,
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

        private MerklePath GetTransactionMerklePathAndRoot(Transaction transaction, out Hash root)
        {
            var fakeHash1 = HashHelper.ComputeFrom("fake1");
            var fakeHash2 = HashHelper.ComputeFrom("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = HashHelper.ComputeFrom(rawBytes);
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
            Assert.True(result.Status == TransactionResultStatus.Mined, result.Error);

            var parentChainHeight = await GetParentChainHeight(tester, crossChainContract);
            parentChainHeight.ShouldBe(height);
        }

        private async Task<Hash> IndexMainChainBlockAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester,
            Address consensusContract, Address crossChainContract, Address parliamentContract, Transaction transaction,
            long height,
            long sideHeight,
            List<Hash> indexedSideChainBlockRoots)
        {
            var fakeHash1 = HashHelper.ComputeFrom("fake1");
            var fakeHash2 = HashHelper.ComputeFrom("fake2");

            var rawBytes = transaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString())).ToArray();
            var hash = HashHelper.ComputeFrom(rawBytes);
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
                    CreateParentChainBlockData(i, MainChainId, HashHelper.ConcatAndCompute(fakeHash1, fakeHash2)));
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
            Assert.True(result.Status == TransactionResultStatus.Mined, result.Error);

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
        
        #endregion
    }
}