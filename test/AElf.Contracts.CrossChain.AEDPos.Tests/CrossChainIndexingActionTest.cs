using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.CrossChain.AEDPos.Tests
{
    public class CrossChainIndexingActionTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task RecordCrossChainData()
        {
            int parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Mined);
        }

        #region Parent chain

        [Fact]
        public async Task RecordParentChainData()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);

            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RecordParentChainData_Twice()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };
            var tx = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(tx.TransactionResult.Status == TransactionResultStatus.Mined);

            var tx2 = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(tx2.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task RecordParentChainData_WrongParentChainId()
        {
            int parentChainId = 123;
            await InitAndCreateSideChainAsync(parentChainId);
            int fakeParentChainId = 124;
            var parentChainBlockData = CreateParentChainBlockData(1, fakeParentChainId, null);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task RecordParentChainData_WrongHeight()
        {
            int parentChainId = 123;
            await InitAndCreateSideChainAsync(parentChainId);
            var parentChainBlockData = CreateParentChainBlockData(0, parentChainId, null);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task RecordParentChainData_ContinuousData()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            Hash fakeTransactionStatusMerkleRoot1 = Hash.FromString("TransactionStatusMerkleRoot1");
            var parentChainBlockData1 = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot1);

            Hash fakeTransactionStatusMerkleRoot2 = Hash.FromString("TransactionStatusMerkleRoot2");
            var parentChainBlockData2 = CreateParentChainBlockData(parentChainHeightOfCreation + 1, parentChainId,
                fakeTransactionStatusMerkleRoot2);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RecordParentChainData_DiscontinuousData()
        {
            int parentChainId = 123;
            await InitAndCreateSideChainAsync(parentChainId);
            var parentChainBlockData1 = CreateParentChainBlockData(1, parentChainId, null);

            var parentChainBlockData2 = CreateParentChainBlockData(3, parentChainId, null);

            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task GetParentChainHeight()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            var tx = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(tx.TransactionResult.Status == TransactionResultStatus.Mined);

            var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
            Assert.True(parentChainHeightOfCreation == height.Value);
        }

        [Fact]
        public async Task GetParentChainHeight_WithoutIndexing()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
            Assert.Equal(parentChainHeightOfCreation - 1, height.Value);
        }

        [Fact]
        public async Task RechargeForSideChain()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };

            //without enough token
            var txResult = (await CrossChainContractStub.Recharge.SendAsync(rechargeInput)).TransactionResult;
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Insufficient allowance").ShouldBeTrue();

            //with enough token
            await ApproveBalanceAsync(100_000L);
            txResult = (await CrossChainContractStub.Recharge.SendAsync(rechargeInput)).TransactionResult;
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task RechargeForSideChain_Terminated()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            await ApproveBalanceAsync(100_000L);

            var proposalId = await DisposalSideChainProposalAsync(new SInt32Value {Value = sideChainId});
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };

            var chainStatus =
                await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = sideChainId});
            Assert.True(chainStatus.Value == (int) SideChainStatus.Terminated);

            var txResult = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.TransactionResult.Error.Contains("Side chain not found or not able to be recharged.")
                .ShouldBeTrue();
        }

        [Fact]
        public async Task RechargeForSideChain_ChainNoExist()
        {
            var parentChainId = 123;
            long lockedTokenAmount = 10;
            await InitializeCrossChainContractAsync(parentChainId);

            await ApproveBalanceAsync(lockedTokenAmount);
            var otherChainId = ChainHelper.GetChainId(5);
            var rechargeInput = new RechargeInput()
            {
                ChainId = otherChainId,
                Amount = 100_000L
            };
            await ApproveBalanceAsync(100_000L);
            var txResult = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);

            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.TransactionResult.Error.Contains("Side chain not found or not able to be recharged.")
                .ShouldBeTrue();
        }

        #endregion

        #region Side Chain

        [Fact]
        public async Task RecordSideChainData()
        {
            var parentChainId = 123;
            var lockedToken = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };
            var indexingTx = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);

            Assert.True(indexingTx.TransactionResult.Status == TransactionResultStatus.Mined);
            
            var balance = await CrossChainContractStub.LockedBalance.CallAsync(new SInt32Value {Value = sideChainId});
            Assert.Equal(lockedToken - 1, balance.Value);
            
            var blockHeader = await BlockchainService.GetBestChainLastBlockHeaderAsync();
            var indexedCrossChainBlockData = await CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight.
                CallAsync(new SInt64Value {Value = blockHeader.Height});
            
            Assert.Equal(crossChainBlockData, indexedCrossChainBlockData);
        }
        
        [Fact]
        public async Task RecordSideChainData_WithChainNotExist()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            var sideChainId1 =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            // create second side chain
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);
            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.Empty);

            var sideChainId2 = ChainHelper.GetChainId(2);
            
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.CreateSideChain.GetTransaction(sideChainCreationRequest)
            });

            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId1, fakeTxMerkleTreeRoot);

            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId2, fakeTxMerkleTreeRoot);
            int fakeChainId = 124;

            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, fakeChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.RecordCrossChainData.GetTransaction(crossChainBlockData)
            });

            var balance = await CrossChainContractStub.LockedBalance.CallAsync(new SInt32Value {Value = sideChainId1});
            Assert.Equal(lockedToken - 1, balance.Value);
            
            var blockHeader = await BlockchainService.GetBestChainLastBlockHeaderAsync();
            var indexedCrossChainBlockData = await CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight.
                CallAsync(new SInt64Value {Value = blockHeader.Height});
            
            var expectedCrossChainBlocData = new CrossChainBlockData();
            expectedCrossChainBlocData.SideChainBlockData.Add(sideChainBlockData1);
            Assert.Equal(expectedCrossChainBlocData, indexedCrossChainBlockData);
        }
        
         [Fact]
        public async Task RecordCrossChainData_WithChainInsufficientBalance()
        {
            int parentChainId = 123;
            long lockedToken = 2;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };
            
            var indexingRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(indexingRes.TransactionResult.Status == TransactionResultStatus.Mined);

            var fakeSideChainBlockHash2 = Hash.FromString("sideChainBlockHash2");
            var fakeTxMerkleTreeRoot2 = Hash.FromString("txMerkleTreeRoot2");

            sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash2, 2, sideChainId, fakeTxMerkleTreeRoot2);

            crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };
            
            var indexingTx2 = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(indexingTx2.TransactionResult.Status == TransactionResultStatus.Mined);
            
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value {Value = sideChainId});
            Assert.Equal((int) SideChainStatus.Terminated, chainStatus.Value);
        }

        [Fact]
        public async Task GetChainInitializationContext_Success()
        {
            var parentChainId = 123;
            var lockedToken = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };

            var indexingRes = await CrossChainContractStub.RecordCrossChainData.SendAsync(crossChainBlockData);
            Assert.True(indexingRes.TransactionResult.Status == TransactionResultStatus.Mined);

            //not exsit chain id
            var chainInitializationContext =
                await CrossChainContractStub.GetChainInitializationData.CallAsync(new SInt32Value
                    {Value = parentChainId});
            chainInitializationContext.ChainId.ShouldBe(0);
            chainInitializationContext.Creator.ShouldBeNull();

            //valid chain id
            chainInitializationContext =
               await CrossChainContractStub.GetChainInitializationData.CallAsync(new SInt32Value {Value = sideChainId});
            chainInitializationContext.ChainId.ShouldBe(sideChainId);
            chainInitializationContext.Creator.ShouldBe(Address.FromPublicKey(DefaultKeyPair.PublicKey));
        }

        #endregion

        #region Verification

        [Fact]
        public async Task CrossChain_MerklePath()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var transactionId = Hash.FromString("sideChainBlockHash");
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            binaryMerkleTree.AddNodes(new[] {transactionId, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(binaryMerkleTree.GenerateMerklePath(0));
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);

            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.RecordCrossChainData.GetTransaction(crossChainBlockData)
            });
            
            var crossChainMerkleProofContext =
                await CrossChainContractStub.GetBoundParentChainHeightAndMerklePathByHeight.CallAsync(new SInt64Value
                    {Value = sideChainHeight});
            Assert.Equal(merklePath.ToByteString(),
                crossChainMerkleProofContext.MerklePathForParentChainRoot.ToByteString());
            var calculatedRoot = crossChainMerkleProofContext.MerklePathForParentChainRoot.Path
                .ComputeBinaryMerkleTreeRootWithPathAndLeafNode(transactionId);
            Assert.Equal(merkleTreeRoot, calculatedRoot);
        }

        [Fact]
        public async Task CrossChain_Verification()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var txId = Hash.FromString("sideChainBlockHash");
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = txId.ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString()))
                .ToArray();
            var hash = Hash.FromRawBytes(rawBytes);

            binaryMerkleTree.AddNodes(new[] {hash, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            var merklePath = new MerklePath();
            merklePath.Path.AddRange(binaryMerkleTree.GenerateMerklePath(0));
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
            {
                SideChainTransactionsRoot = merkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData}
            };

            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                CrossChainContractStub.RecordCrossChainData.GetTransaction(crossChainBlockData)
            });

            var verificationInput = new VerifyTransactionInput()
            {
                TransactionId = txId,
                ParentChainHeight = parentChainHeightOfCreation
            };
            verificationInput.Path.AddRange(merklePath.Path);
            
            var txRes = await CrossChainContractStub.VerifyTransaction.SendAsync(verificationInput);
            var verified = BoolValue.Parser.ParseFrom(txRes.TransactionResult.ReturnValue).Value;
            Assert.True(verified);
        }
        
        
        [Fact]
        public async Task CurrentSideChainSerialNumber()
        {
            var parentChainId = 123;
            var lockedToken = 100_000L;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            
            var serialNumber = await CrossChainContractStub.CurrentSideChainSerialNumber.CallAsync(new Empty());
            serialNumber.Value.ShouldBeGreaterThanOrEqualTo(0);
        }
        
        [Fact]
        public async Task LockedToken_Verification()
        {
            var parentChainId = 123;
            var lockedToken = 100_000L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            
            var lockedToken1 =
                await CrossChainContractStub.LockedToken.CallAsync(new SInt32Value {Value = sideChainId});
            lockedToken1.Value.ShouldBe(lockedToken);
            
            var address = await CrossChainContractStub.LockedAddress.CallAsync(new SInt32Value {Value = sideChainId});
            address.ShouldBe(Address.FromPublicKey(DefaultKeyPair.PublicKey));
        }
        #endregion
        
        #region HelpMethods

        private SideChainBlockData CreateSideChainBlockData(Hash blockHash, long height, int sideChainId,
            Hash txMerkleTreeRoot)
        {
            return new SideChainBlockData
            {
                BlockHeaderHash = blockHash,
                Height = height,
                ChainId = sideChainId,
                TransactionMerkleTreeRoot = txMerkleTreeRoot
            };
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