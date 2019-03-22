using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
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
                TransactionMerkleTreeRoot = fakeTxMerkleTreeRoot
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

            var parentChainIdInState = await Tester.CallContractMethodAsync(
                CrossChainContractAddress,
                CrossChainConsts.GetParentChainIdMethodName, new Empty());
            
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.CrossChainIndexingMethodName, null,
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction>{tx});
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
            int parentChainId = 123;
            long parentChainHeight = 0;
            await InitAndCreateSideChain(parentChainId);
            
            var height = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.GetParentChainHeight),
                new Empty())).Value;
            Assert.True(parentChainHeight == height);
        }
        
        #endregion

        #region Side chain

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
                TransactionMerkleTreeRoot = fakeTxMerkleTreeRoot
            };
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = { sideChainBlockData}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var indexingRes = await Tester.GetTransactionResultAsync(indexingTx.GetHash());
            Assert.True(indexingRes.Status == TransactionResultStatus.Mined);
            var balance =SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.LockedBalance),
                new SInt32Value()
                {
                    Value = sideChainId
                })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                nameof (CrossChainContract.GetIndexedCrossChainBlockDataByHeight),
                new SInt64Value()
                {
                    Value = block.Height
                }));
            Assert.Equal(crossChainBlockData, indexedCrossChainBlockData);
        }
        
        [Fact]
        public async Task RecordSideChainData_WithChainNotExist()
        {
            int parentChainId = 123;
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
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var sideChainId2 = ChainHelpers.GetChainId(2);
            var tx2 = await GenerateTransactionAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain),
                null,
                new SInt32Value()
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
                TransactionMerkleTreeRoot = fakeTxMerkleTreeRoot
            };
            
            var sideChainBlockData2 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 2, // wrong height
                SideChainId = sideChainId2,
                TransactionMerkleTreeRoot = fakeTxMerkleTreeRoot
            };

            int fakeChainId = 124;
            
            var sideChainBlockData3 = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = fakeChainId,
                TransactionMerkleTreeRoot = fakeTxMerkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = { sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var balance = SInt64Value.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContract.LockedBalance),
                    new SInt32Value()
                    {
                        Value = sideChainId1
                    })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData =CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.GetIndexedCrossChainBlockDataByHeight),
                new SInt64Value()
                {
                    Value = block.Height
                }));
            var expectedCrossChainBlocData = new CrossChainBlockData();
            expectedCrossChainBlocData.SideChainBlockData.Add(sideChainBlockData1);
            Assert.Equal(expectedCrossChainBlocData, indexedCrossChainBlockData);
        }

        #endregion

        #region Verification

        [Fact]
        public async Task CrossChain_MerklePath()
        {
            int parentChainId = 123;
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
            
            var crossChainMerkleProofContext = CrossChainMerkleProofContext.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContract.GetBoundParentChainHeightAndMerklePathByHeight),
                new SInt64Value()
                {
                    Value = sideChainHeight
                }));
            Assert.Equal(merklePath, crossChainMerkleProofContext.MerklePathForParentChainRoot);
            Assert.Equal(merkleTreeRoot, crossChainMerkleProofContext.MerklePathForParentChainRoot.ComputeRootWith(txHash));
        }

        [Fact]
        public async Task CrossChain_Verification()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            var sideChainId = await InitAndCreateSideChain(parentChainId, lockedToken);
            var txId = Hash.FromString("sideChainBlockHash");
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var hash = Hash.FromTwoHashes(txId, Hash.FromString(TransactionResultStatus.Mined.ToString()));
            
            binaryMerkleTree.AddNodes(new[] {hash, fakeHash1, fakeHash2});
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
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };
            
            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConsts.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            
            var txRes = await ExecuteContractWithMiningAsync(
                CrossChainContractAddress,
                nameof(CrossChainContract.VerifyTransaction),
                new VerifyTransactionInput()
                {
                    TransactionId = txId,
                    MerklePath = merklePath,
                    ParentChainHeight = parentChainHeight
                });
            var verified = BoolValue.Parser.ParseFrom(txRes.ReturnValue).Value;
            Assert.True(verified);
        }
       
        #endregion

        #region Cross chain transfer.
        
        // todo : Move this to token contract tests.
        
        [Fact]
        public async Task CrossChainTransfer()
        {
            int toChainId = 123;
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContract.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    ToChainId = toChainId,
                    Amount = 100_000,
                    Symbol = "ELF",
                    To = Tester.GetCallOwnerAddress()
                });
            await Tester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await Tester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);
            
            var balanceResult = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Owner = Tester.GetCallOwnerAddress(),
                    Symbol = "ELF"
                });
            var balance = GetBalanceOutput.Parser.ParseFrom(balanceResult);
            Assert.True(balance.Balance == Tester.InitialBalanceOfStarter - 100_000);
        }
        
        [Fact]
        public void CrossChainReceive(){}
        

        #endregion
    }
}