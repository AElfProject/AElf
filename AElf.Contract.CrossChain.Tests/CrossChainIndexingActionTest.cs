using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainIndexingActionTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task RecordCrossChainData()
        {
            int parentChainId = 123;
            var sideChainId = await InitAndCreateSideChain(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = sideChainId,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData},
                SideChainBlockData = { sideChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Mined);
        }
        
        #region Parent chain

        [Fact]
        public async Task RecordParentChainData()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task RecordParentChainData_Twice()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var tx = GenerateTransaction(CrossChainContractAddress, CrossChainConsts.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction>{tx});
            (await GetTransactionResult(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);
            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task RecordParentChainData_WrongParentChainId()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            int fakeParentChainId = 124;
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = fakeParentChainId,
                    ParentChainHeight = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task RecordParentChainData_WrongHeight()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 0
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task RecordParentChainData_ContinuousData()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData1 = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            
            var parentChainBlockData2 = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 2
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task RecordParentChainData_DiscontinuousData()
        {
            int parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData1 = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            
            var parentChainBlockData2 = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 3
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);
            
            Assert.True(txRes.Status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task GetParentChainHeight()
        {
            int parentChainId = 123;
            long parentChainHeight = 1;
            await InitAndCreateSideChain(parentChainId);
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = parentChainHeight
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var tx = GenerateTransaction(CrossChainContractAddress, CrossChainConsts.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction>{tx});
            (await GetTransactionResult(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);

            var bytes = await CallContractMethodAsync(CrossChainContractAddress, CrossChainConsts.GetParentChainHeightMethodName);
            Assert.True(parentChainHeight == bytes.DeserializeToInt64());
        }
        
        [Fact]
        public async Task GetParentChainHeight_WithoutIndexing()
        {
            int parentChainId = 123;
            ulong parentChainHeight = 0;
            await InitAndCreateSideChain(parentChainId);
            
            var bytes = await CallContractMethodAsync(CrossChainContractAddress, CrossChainConsts.GetParentChainHeightMethodName);
            Assert.True(parentChainHeight == bytes.DeserializeToUInt64());
        }
        
        #endregion

        #region Side chain

        [Fact]
        public async Task RecordSideChainData()
        {
            int parentChainId = 123;
            ulong lockedToken = 10;
            var sideChainId = await InitAndCreateSideChain(parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = sideChainId,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = { sideChainBlockData}
            };

            var indexingTx = GenerateTransaction(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var balance = await CallContractMethodAsync(CrossChainContractAddress,
                CrossChainConsts.GetLockedBalanceMethodName, sideChainId);
            Assert.True(balance.DeserializeToUInt64() == lockedToken - 1);

            var indexedCrossChainBlockData = await CallContractMethodAsync(CrossChainContractAddress,
                CrossChainConsts.GetIndexedCrossChainBlockDataByHeight, block.Height);
            var deserializedCrossChainBlockData =
                indexedCrossChainBlockData.DeserializeToPbMessage<CrossChainBlockData>();
            Assert.Equal(crossChainBlockData, deserializedCrossChainBlockData);
        }
        
        [Fact]
        public async Task RecordSideChainData_WithChainNotExist()
        {
            int parentChainId = 123;
            ulong lockedToken = 10;
            var sideChainId1 = await InitAndCreateSideChain(parentChainId, lockedToken);
            
            // create second side chain
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = GenerateTransaction(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var sideChainId2 = ChainHelpers.GetChainId(2);
            var tx2 = GenerateTransaction(CrossChainContractAddress, "CreateSideChain", null, sideChainId2);
            await MineAsync(new List<Transaction> {tx2});
            
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = sideChainId1,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            
            var sideChainBlockData2 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 2, // wrong height
                SideChainId = sideChainId2,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };

            int fakeChainId = 124;
            
            var sideChainBlockData3 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = fakeChainId,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = { sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            var indexingTx = GenerateTransaction(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            
            var balance = await CallContractMethodAsync(CrossChainContractAddress,
                CrossChainConsts.GetLockedBalanceMethodName, sideChainId1);
            Assert.True(balance.DeserializeToUInt64() == lockedToken - 1);

            var indexedCrossChainBlockData = await CallContractMethodAsync(CrossChainContractAddress,
                CrossChainConsts.GetIndexedCrossChainBlockDataByHeight, block.Height);
            var deserializedCrossChainBlockData =
                indexedCrossChainBlockData.DeserializeToPbMessage<CrossChainBlockData>();
            var expectedCrossChainBlocData = new CrossChainBlockData();
            expectedCrossChainBlocData.SideChainBlockData.Add(sideChainBlockData1);
            Assert.Equal(expectedCrossChainBlocData, deserializedCrossChainBlockData);
        }

        #endregion

        #region Verification

        [Fact]
        public async Task CrossChain_MerklePath()
        {
            int parentChainId = 123;
            ulong lockedToken = 10;
            var sideChainId = await InitAndCreateSideChain(parentChainId, lockedToken);
            var txHash = Hash.FromString("sideChainBlockHash");
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            binaryMerkleTree.AddNodes(new[] {txHash, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            var merklePath = binaryMerkleTree.GenerateMerklePath(0);
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainHeight = 1,
                    ParentChainId = parentChainId,
                    SideChainTransactionsRoot = merkleTreeRoot
                }
            };
            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };
            
            var indexingTx = GenerateTransaction(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            
            var serializedMerklePath = await CallContractMethodAsync(CrossChainContractAddress,
                CrossChainConsts.GetMerklePathByHeightMethodName, sideChainHeight);
            var deserializedMerklePath = serializedMerklePath.DeserializeToPbMessage<MerklePath>();
            Assert.Equal(merklePath, deserializedMerklePath);
            Assert.Equal(merkleTreeRoot, deserializedMerklePath.ComputeRootWith(txHash));
        }

        [Fact]
        public async Task CrossChain_Verification()
        {
            int parentChainId = 123;
            ulong lockedToken = 10;
            var sideChainId = await InitAndCreateSideChain(parentChainId, lockedToken);
            var txHash = Hash.FromString("sideChainBlockHash");
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            binaryMerkleTree.AddNodes(new[] {txHash, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            var merklePath = binaryMerkleTree.GenerateMerklePath(0);
            var parentChainHeight = 1;
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainHeight = parentChainHeight,
                    ParentChainId = parentChainId,
                    SideChainTransactionsRoot = merkleTreeRoot
                }
            };
            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };
            
            var indexingTx = GenerateTransaction(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            
            var merklePathForFakeHash1 = binaryMerkleTree.GenerateMerklePath(1);
            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.VerifyTransactionMethodName, fakeHash1, merklePathForFakeHash1, parentChainHeight);
            Assert.True(BitConverter.ToBoolean(txRes.ReturnValue.ToByteArray()));
        }
        #endregion
    }
}