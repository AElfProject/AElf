using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.CSharp.Core.Utils;
using AElf.Types;
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
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
//            var parentChainBlockData = new ParentChainBlockData
//            {
//                ParentChainId = parentChainId,
//                ParentChainHeight = 1,
//                TransactionStatusMerkleRoot = fakeTxMerkleTreeRoot
//            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Mined);
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

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Mined);
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction> {tx});
            (await GetTransactionResultAsync(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);
            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Failed);
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

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Failed);
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

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Failed);
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

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Mined);
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

            var txRes = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, crossChainBlockData);

            Assert.True(txRes.Status == TransactionResultStatus.Failed);
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

            var tx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null,
                crossChainBlockData);
            await MineAsync(new List<Transaction> {tx});
            (await GetTransactionResultAsync(tx.GetHash())).Status.ShouldBe(TransactionResultStatus.Mined);

            var height = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetParentChainHeight),
                new Empty())).Value;
            Assert.True(parentChainHeightOfCreation == height);
        }

        [Fact]
        public async Task GetParentChainHeight_WithoutIndexing()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            var height = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(
                CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetParentChainHeight),
                new Empty())).Value;
            Assert.Equal(parentChainHeightOfCreation - 1, height);
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
            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient allowance").ShouldBeTrue();

            //with enough token
            await ApproveBalanceAsync(100_000L);
            transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
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

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value {Value = sideChainId})).Value;
            Assert.True(chainStatus == (int) SideChainStatus.Terminated);

            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Side chain not found or not able to be recharged.").ShouldBeTrue();
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
            var transactionResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Recharge),
                rechargeInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Side chain not found or not able to be recharged.").ShouldBeTrue();
        }

        #endregion

        #region Side chain

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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var indexingRes = await Tester.GetTransactionResultAsync(indexingTx.GetHash());
            Assert.True(indexingRes.Status == TransactionResultStatus.Mined);
            var balance = SInt64Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                new SInt32Value()
                {
                    Value = sideChainId
                })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight),
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
            long parentChainHeightOfCreation = 10;
            var sideChainId1 =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            // create second side chain
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);
            var sideChainCreationRequest = CreateSideChainCreationRequest(1, lockedTokenAmount, ByteString.Empty);

            var sideChainId2 = ChainHelper.GetChainId(2);
            var tx2 = await GenerateTransactionAsync(
                CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.CreateSideChain),
                null,
                new SInt32Value()
                {
                    Value = sideChainId2
                });
            await MineAsync(new List<Transaction> {tx2});

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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var balance = SInt64Value.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.LockedBalance),
                    new SInt32Value()
                    {
                        Value = sideChainId1
                    })).Value;
            Assert.Equal(lockedToken - 1, balance);

            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight),
                    new SInt64Value()
                    {
                        Value = block.Height
                    }));
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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var indexingRes = await Tester.GetTransactionResultAsync(indexingTx.GetHash());
            Assert.True(indexingRes.Status == TransactionResultStatus.Mined);

            var fakeSideChainBlockHash2 = Hash.FromString("sideChainBlockHash2");
            var fakeTxMerkleTreeRoot2 = Hash.FromString("txMerkleTreeRoot2");

            sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash2, 2, sideChainId, fakeTxMerkleTreeRoot2);

            crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockData}
            };

            var indexingTx2 = await ExecuteContractWithMiningAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData), crossChainBlockData);
            Assert.True(indexingTx2.Status == TransactionResultStatus.Mined);

            var chainStatus = SInt32Value.Parser.ParseFrom(await CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.GetChainStatus),
                new SInt32Value()
                {
                    Value = sideChainId
                })).Value;
            Assert.Equal((int) SideChainStatus.Terminated, chainStatus);
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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});
            var indexingRes = await Tester.GetTransactionResultAsync(indexingTx.GetHash());
            Assert.True(indexingRes.Status == TransactionResultStatus.Mined);

            //not exsit chain id
            var chainInitializationContext = ChainInitializationData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.GetChainInitializationData),
                    new SInt32Value()
                    {
                        Value = parentChainId
                    }));
            chainInitializationContext.ChainId.ShouldBe(0);
            chainInitializationContext.Creator.ShouldBeNull();

            //valid chain id
            chainInitializationContext = ChainInitializationData.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.GetChainInitializationData),
                    new SInt32Value()
                    {
                        Value = sideChainId
                    }));
            chainInitializationContext.ChainId.ShouldBe(sideChainId);
            chainInitializationContext.Creator.ShouldBe(Address.FromPublicKey(Tester.KeyPair.PublicKey));
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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var crossChainMerkleProofContext = CrossChainMerkleProofContext.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub
                        .GetBoundParentChainHeightAndMerklePathByHeight),
                    new SInt64Value()
                    {
                        Value = sideChainHeight
                    }));
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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            var block = await MineAsync(new List<Transaction> {indexingTx});

            var verificationInput = new VerifyTransactionInput()
            {
                TransactionId = txId,
                ParentChainHeight = parentChainHeightOfCreation
            };
            verificationInput.Path.AddRange(merklePath.Path);
            var txRes = await ExecuteContractWithMiningAsync(
                CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.VerifyTransaction), verificationInput);

            var verified = BoolValue.Parser.ParseFrom(txRes.ReturnValue).Value;
            Assert.True(verified);
        }

        [Fact]
        public async Task CurrentSideChainSerialNumber()
        {
            var parentChainId = 123;
            var lockedToken = 100_000L;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var serialNumber = SInt64Value.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.CurrentSideChainSerialNumber),
                    new Empty())).Value;
            serialNumber.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task LockedToken_Verification()
        {
            var parentChainId = 123;
            var lockedToken = 100_000L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var lockedToken1 = SInt64Value.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.LockedToken),
                    new SInt32Value
                    {
                        Value = sideChainId
                    })).Value;
            lockedToken1.ShouldBe(lockedToken);

            var address = Address.Parser.ParseFrom(
                await CallContractMethodAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.LockedAddress),
                    new SInt32Value
                    {
                        Value = sideChainId
                    }));
            address.ShouldBe(Address.FromPublicKey(Tester.KeyPair.PublicKey));
        }

        #endregion

        #region Cross chain transfer.

        // todo : Move these cases to token contract tests.

        [Fact]
        public async Task CrossChainTransfer()
        {
            int toChainId = 123;
            var tokenInfoResult = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = "ELF"
                });
            var tokenInfo = TokenInfo.Parser.ParseFrom(tokenInfoResult);
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    ToChainId = toChainId,
                    Amount = 100_000,
                    TokenInfo = tokenInfo,
                    To = Tester.GetCallOwnerAddress()
                });
            await Tester.MineAsync(new List<Transaction> {crossChainTransferTransaction});
            var txResult = await Tester.GetTransactionResultAsync(crossChainTransferTransaction.GetHash());
            Assert.True(txResult.Status == TransactionResultStatus.Mined);

            var balanceResult = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput
                {
                    Owner = Tester.GetCallOwnerAddress(),
                    Symbol = "ELF"
                });
            var balance = GetBalanceOutput.Parser.ParseFrom(balanceResult);
            Assert.True(balance.Balance == Tester.InitialBalanceOfStarter - 100_000);
        }

        [Fact]
        public async Task CrossChainReceiveToken()
        {
            int parentChainId = 123;
            int chainId1 = ChainHelper.ConvertBase58ToChainId("AELF");
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            var sidechainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var tokenInfoResult = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo), new GetTokenInfoInput
                {
                    Symbol = "ELF"
                });
            var tokenInfo = TokenInfo.Parser.ParseFrom(tokenInfoResult);
            var transferAmount = 100_000;
            var balanceBeforeTransfer = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = Tester.GetCallOwnerAddress(),
                    Symbol = "ELF"
                });
            var balanceBefore = GetBalanceOutput.Parser.ParseFrom(balanceBeforeTransfer).Balance;
            var receiver = AddressHelper.StringToAddress("CrossChainReceiver");
            var crossChainTransferTransaction = await GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer), null, new CrossChainTransferInput
                {
                    ToChainId = chainId1,
                    Amount = transferAmount,
                    TokenInfo = tokenInfo,
                    To = receiver
                });
            var binaryMerkleTree = new BinaryMerkleTree();
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = crossChainTransferTransaction.GetHash().ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString())).ToArray();
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

            var indexingTx = await GenerateTransactionAsync(CrossChainContractAddress,
                CrossChainConstants.CrossChainIndexingMethodName, null, crossChainBlockData);
            await MineAsync(new List<Transaction> {indexingTx});
            int chainId2 = ChainHelper.ConvertBase58ToChainId("2113");
            var crossChainReceiveTokenInput = new CrossChainReceiveTokenInput
            {
                FromChainId = chainId2,
                ParentChainHeight = parentChainHeightOfCreation,
                TransferTransactionBytes = crossChainTransferTransaction.ToByteString(),
            };
            crossChainReceiveTokenInput.MerklePath.AddRange(merklePath.Path);
            var txRes = await ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken), crossChainReceiveTokenInput);
            Assert.True(txRes.Status == TransactionResultStatus.Mined);
            var balanceAfterTransfer = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = receiver,
                    Symbol = "ELF"
                });
            var balanceAfter = GetBalanceOutput.Parser.ParseFrom(balanceAfterTransfer).Balance;
            Assert.Equal(transferAmount, balanceAfter);
        }

        #endregion

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
    }
}