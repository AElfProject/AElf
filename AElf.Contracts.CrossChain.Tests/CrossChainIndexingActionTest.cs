using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainIndexingActionTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task CrossChain_MerklePath()
        {
            var parentChainId = 123;
            long lockedToken = 10;
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
                    ParentChainId = parentChainId
                    //SideChainTransactionsRoot = merkleTreeRoot
                }
            };
            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var deserializedMerklePath = MerklePath.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.GetMerklePathByHeight),
                new SInt64Value
                {
                    Value = sideChainHeight
                }));
            Assert.Equal(merklePath, deserializedMerklePath);
            Assert.Equal(merkleTreeRoot, deserializedMerklePath.ComputeRootWith(txHash));
        }

        [Fact]
        public async Task CrossChain_Verification()
        {
            var parentChainId = 123;
            long lockedToken = 10;
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
                    CrossChainExtraData = new CrossChainExtraData
                    {
                        SideChainTransactionsRoot = merkleTreeRoot
                    }
                }
            };
            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var merklePathForFakeHash1 = binaryMerkleTree.GenerateMerklePath(1);
            var txRes = await ExecuteContractWithMiningAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.VerifyTransaction),
                new VerifyTransactionInput
                {
                    TransactionId = fakeHash1,
                    MerklePath = merklePathForFakeHash1,
                    ParentChainHeight = parentChainHeight
                });
            var verified = BoolValue.Parser.ParseFrom(txRes.ReturnValue).Value;
            Assert.True(verified);
        }

        [Fact]
        public async Task GetParentChainHeight()
        {
            var parentChainId = 123;
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction> {tx});
            (await GetTransactionResult(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);

            var height = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.GetParentChainHeight),
                new Empty())).Value;
            Assert.True(parentChainHeight == height);
        }

        [Fact]
        public async Task GetParentChainHeight_WithoutIndexing()
        {
            var parentChainId = 123;
            long parentChainHeight = 0;
            await InitAndCreateSideChain(parentChainId);

            var height = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.GetParentChainHeight),
                new Empty())).Value;
            Assert.True(parentChainHeight == height);
        }

        [Fact]
        public async Task RecordCrossChainData()
        {
            var parentChainId = 123;
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
                SideChainBlockData = {sideChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RecordParentChainData()
        {
            var parentChainId = 123;
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

            var parentChainIdInState = await Tester.CallContractMethodAsync(
                CrossChainContractAddress,
                CrossChainConsts.GetParentChainIdMethodName, new Empty());

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RecordParentChainData_ContinuousData()
        {
            var parentChainId = 123;
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
            var parentChainId = 123;
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
        public async Task RecordParentChainData_Twice()
        {
            var parentChainId = 123;
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction> {tx});
            (await GetTransactionResult(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);
            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task RecordParentChainData_WrongHeight()
        {
            var parentChainId = 123;
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
        public async Task RecordParentChainData_WrongParentChainId()
        {
            var parentChainId = 123;
            await InitAndCreateSideChain(parentChainId);
            var fakeParentChainId = 124;
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
        public async Task RecordSideChainData()
        {
            var parentChainId = 123;
            var lockedToken = 10L;
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
                SideChainBlockData = {sideChainBlockData}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var indexingRes = await Tester.GetTransactionResultAsync(indexingTx.GetHash());
            Assert.True(indexingRes.Status == TransactionResultStatus.Mined);
            var balance = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.LockedBalance),
                new SInt32Value
                {
                    Value = sideChainId
                })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.GetIndexedCrossChainBlockDataByHeight),
                    new SInt64Value
                    {
                        Value = block.Height
                    }));
            Assert.Equal(crossChainBlockData, indexedCrossChainBlockData);
        }

        [Fact]
        public async Task RecordSideChainData_WithChainNotExist()
        {
            var parentChainId = 123;
            long lockedToken = 10;
            var sideChainId1 = await InitAndCreateSideChain(parentChainId, lockedToken);

            // create second side chain
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };

            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var sideChainId2 = ChainHelpers.GetChainId(2);
            var tx2 = await GenerateTransactionAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain),
                null,
                new SInt32Value
                {
                    Value = sideChainId2
                });
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

            var fakeChainId = 124;

            var sideChainBlockData3 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = fakeChainId,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var balance = SInt64Value.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.LockedBalance),
                    new SInt32Value
                    {
                        Value = sideChainId1
                    })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.GetIndexedCrossChainBlockDataByHeight),
                    new SInt64Value
                    {
                        Value = block.Height
                    }));
            var expectedCrossChainBlocData = new CrossChainBlockData();
            expectedCrossChainBlocData.SideChainBlockData.Add(sideChainBlockData1);
            Assert.Equal(expectedCrossChainBlocData, indexedCrossChainBlockData);
        }
    }
}