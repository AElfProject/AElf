using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKit;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.CrossChain.Tests
{
    public class CrossChainIndexingActionTest : CrossChainContractTestBase
    {
        #region Propose

        [Fact]
        public async Task ProposeCrossChainData_Twice()
        {
            int parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData}
            };

            {
                var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
                Assert.Equal(TransactionResultStatus.Mined, txRes.TransactionResult.Status);
            }

            {
                var txRes =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
                Assert.Equal(TransactionResultStatus.Failed, txRes.TransactionResult.Status);
            }
        }

        [Fact]
        public async Task ProposeCrossChainData_NotAuthorized()
        {
            int parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData}
            };

            {
                var txRes = await GetCrossChainContractStub(SampleECKeyPairs.KeyPairs.Last()).ProposeCrossChainIndexing
                    .SendWithExceptionAsync(crossChainBlockData);
                Assert.Equal(TransactionResultStatus.Failed, txRes.TransactionResult.Status);
            }
        }

        //        #region Parent chain

        [Fact]
        public async Task ProposeCrossChainData_ParentChainBlockData()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            var proposedCrossChainBlockData = CrossChainIndexingDataProposedEvent.Parser
                .ParseFrom(txRes.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(CrossChainIndexingDataProposedEvent))).NonIndexed)
                .ProposedCrossChainData;
            Assert.NotNull(proposedCrossChainBlockData);
            Assert.Equal(crossChainBlockData, proposedCrossChainBlockData);

            {
                var pendingProposal =
                    await CrossChainContractStub.GetPendingCrossChainIndexingProposal.CallAsync(new Empty());
                Assert.Equal(proposalId, pendingProposal.ProposalId);
                Assert.Equal(DefaultSender, pendingProposal.Proposer);
                Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
                Assert.False(pendingProposal.ToBeReleased);
            }
        }

        [Fact]
        public async Task ProposeCrossChainData_WrongParentChainId()
        {
            int parentChainId = 123;
            await InitAndCreateSideChainAsync(parentChainId);
            int fakeParentChainId = 124;
            var parentChainBlockData = CreateParentChainBlockData(1, fakeParentChainId, null);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            var txRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task ProposeParentChainData_WrongHeight()
        {
            int parentChainId = 123;
            await InitAndCreateSideChainAsync(parentChainId);
            var parentChainBlockData = CreateParentChainBlockData(0, parentChainId, null);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            var txRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task ProposeParentChainData_ContinuousData()
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
                ParentChainBlockDataList = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            Assert.Equal(TransactionResultStatus.Mined, txRes.TransactionResult.Status);
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
                ParentChainBlockDataList = {parentChainBlockData1, parentChainBlockData2}
            };

            var txRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
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
        public async Task GetParentChainHeight()
        {
            int parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

            {
                var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                Assert.Equal(parentChainHeightOfCreation - 1, height.Value);
            }
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            var tx = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            Assert.True(tx.TransactionResult.Status == TransactionResultStatus.Mined);

            {
                var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                Assert.Equal(parentChainHeightOfCreation - 1, height.Value);
            }
        }

        [Fact]
        public async Task ProposeForSideChain()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);

            var rechargeInput = new RechargeInput()
            {
                ChainId = sideChainId,
                Amount = 100_000L
            };

            {
                //without enough token
                var txResult = (await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput))
                    .TransactionResult;
                Assert.Equal(TransactionResultStatus.Failed, txResult.Status);
                Assert.Contains("Insufficient allowance", txResult.Error);
            }

            {
                //with enough token
                await ApproveBalanceAsync(100_000L);
                var txResult = (await CrossChainContractStub.Recharge.SendAsync(rechargeInput)).TransactionResult;
                Assert.Equal(TransactionResultStatus.Mined, txResult.Status);
            }
        }


        [Fact]
        public async Task RechargeForSideChain_Terminated()
        {
            var parentChainId = 123;
            var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
            await ApproveBalanceAsync(100_000L);

            var proposalId = await DisposeSideChainProposalAsync(new SInt32Value {Value = sideChainId});
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

            var txResult = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);
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
            var txResult = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.TransactionResult.Error.Contains("Side chain not found or not able to be recharged.")
                .ShouldBeTrue();
        }

        [Fact]
        public async Task ProposeSideChainData()
        {
            var parentChainId = 123;
            var lockedToken = 2;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            var proposedCrossChainBlockData = CrossChainIndexingDataProposedEvent.Parser
                .ParseFrom(txRes.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(CrossChainIndexingDataProposedEvent))).NonIndexed)
                .ProposedCrossChainData;
            Assert.NotNull(proposedCrossChainBlockData);
            Assert.Equal(crossChainBlockData, proposedCrossChainBlockData);

            var pendingProposal =
                await CrossChainContractStub.GetPendingCrossChainIndexingProposal.CallAsync(new Empty());
            Assert.Equal(proposalId, pendingProposal.ProposalId);
            Assert.Equal(DefaultSender, pendingProposal.Proposer);
            Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
            Assert.False(pendingProposal.ToBeReleased);
        }

        [Fact]
        public async Task ProposeSideChainData_WithChainNotExist()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            var sideChainId1 =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            
            // create second side chain
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);

            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId1, fakeTxMerkleTreeRoot);
            int fakeChainId = 124;

            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, fakeChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };

            var txRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            txRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed");
        }

        [Fact]
        public async Task ProposeSideChainData_WithChainInsufficientBalance()
        {
            int parentChainId = 123;
            long lockedToken = 2;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            var txResult =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            txResult.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed");
        }
        
        [Fact]
        public async Task ProposeSideChainData_Inconsistent()
        {
            int parentChainId = 123;
            long lockedToken = 5;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 4, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2, sideChainBlockData3}
            };

            var txResult =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            txResult.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed");
        }

        [Fact]
        public async Task GetChainInitializationContext_Success()
        {
            var parentChainId = 123;
            var lockedToken = 10L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            //not exist chain id
            var error =
                await CrossChainContractStub.GetChainInitializationData.CallWithExceptionAsync(new SInt32Value
                    {Value = parentChainId});
            error.Value.ShouldContain("Side chain not found.");

            //valid chain id
            var chainInitializationContext =
                await CrossChainContractStub.GetChainInitializationData.CallAsync(new SInt32Value
                    {Value = sideChainId});
            chainInitializationContext.ChainId.ShouldBe(sideChainId);
            chainInitializationContext.Creator.ShouldBe(Address.FromPublicKey(DefaultKeyPair.PublicKey));
        }

        #endregion

        #region Release

        [Fact]
        public async Task Release_IndexingSideChain_Success()
        {
            var parentChainId = 123;
            var lockedToken = 5;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            
            {
                var pendingProposal =
                    await CrossChainContractStub.GetPendingCrossChainIndexingProposal.CallAsync(new Empty());
                Assert.Equal(proposalId, pendingProposal.ProposalId);
                Assert.Equal(DefaultSender, pendingProposal.Proposer);
                Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
                Assert.False(pendingProposal.ToBeReleased);
            }
            
            await ApproveWithMinersAsync(proposalId);

            {
                var pendingProposal =
                    await CrossChainContractStub.GetPendingCrossChainIndexingProposal.CallAsync(new Empty());
                Assert.Equal(proposalId, pendingProposal.ProposalId);
                Assert.Equal(DefaultSender, pendingProposal.Proposer);
                Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
                Assert.True(pendingProposal.ToBeReleased);
            }
            
            {
                var indexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                indexedHeight.Value.ShouldBe(0);
                
                var balance = await CrossChainContractStub.GetSideChainBalance.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                balance.Value.ShouldBe(lockedToken);
            }
            
            
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var indexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                indexedHeight.Value.ShouldBe(2);
                
                var balance = await CrossChainContractStub.GetSideChainBalance.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                balance.Value.ShouldBe(lockedToken - 2);
            }
            
            {
                await CrossChainContractStub.GetPendingCrossChainIndexingProposal.CallWithExceptionAsync(new Empty());
            }
        }
        
        [Fact]
        public async Task Release_IndexingSideChain_ContinuousTwice()
        {
            var parentChainId = 123;
            var lockedToken = 5;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            await ApproveWithMinersAsync(proposalId);

                        
            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData4 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 4, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData5 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 5, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData6 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 6, sideChainId, fakeTxMerkleTreeRoot);

            var secondCrossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData3, sideChainBlockData4, sideChainBlockData5}
            };

            {
                var secondProposingTxRes =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(
                        secondCrossChainBlockData);
                secondProposingTxRes.TransactionResult.Error.ShouldContain(
                    "Unable to clear cross chain indexing proposal which is not expired.");
            }

            {
                var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
                releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            {
                var secondProposingTxRes =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(new CrossChainBlockData
                    {
                        SideChainBlockDataList = {sideChainBlockData6}
                    });
                secondProposingTxRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
            }
            
            {
                var secondProposingTxRes =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(secondCrossChainBlockData);
                var secondProposalId = ProposalCreated.Parser.ParseFrom(secondProposingTxRes.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
                Assert.NotNull(secondProposalId);
                await ApproveWithMinersAsync(secondProposalId);
                await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(secondProposalId);
                var indexedHeight =
                    await CrossChainContractStub.GetSideChainHeight.CallAsync(new SInt32Value {Value = sideChainId});
                indexedHeight.Value.ShouldBe(5);
                var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                chainStatus.Value.ShouldBe((int) SideChainStatus.Terminated);
            }
        }
        
        [Fact]
        public async Task Release_IndexingSideChain_Terminated()
        {
            var parentChainId = 123;
            var lockedToken = 2;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            await ApproveWithMinersAsync(proposalId);

            {
                var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                chainStatus.Value.ShouldBe((int) SideChainStatus.Active);
            }
            
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new SInt32Value
                {
                    Value = sideChainId
                });
                chainStatus.Value.ShouldBe((int) SideChainStatus.Terminated);
            }
        }

        [Fact]
        public async Task Release_IndexingParentChain_Success()
        {
            var parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            await ApproveWithMinersAsync(proposalId);

            {
                var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                indexedHeight.Value.ShouldBe(parentChainHeightOfCreation - 1);
            }
            
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                indexedHeight.Value.ShouldBe(parentChainHeightOfCreation);
            }
        }
        
        [Fact]
        public async Task Release_IndexingParentChain_ContinuousTwice()
        {
            var parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            Assert.NotNull(proposalId);
            await ApproveWithMinersAsync(proposalId);

            {
                var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                indexedHeight.Value.ShouldBe(parentChainHeightOfCreation - 1);
            }
            
            var parentChainBlockData2 = CreateParentChainBlockData(parentChainHeightOfCreation + 1, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var parentChainBlockData3 = CreateParentChainBlockData(parentChainHeightOfCreation + 2, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var secondCrossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData2, parentChainBlockData3}
            };

            {
                var secondProposingTx =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(secondCrossChainBlockData);
                secondProposingTx.TransactionResult.Error.ShouldContain(
                    "Unable to clear cross chain indexing proposal which is not expired.");
            }
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            {
                var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                indexedHeight.Value.ShouldBe(parentChainHeightOfCreation);
            }

            {
                var secondProposingTx =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(
                        new CrossChainBlockData
                        {
                            ParentChainBlockDataList = {parentChainBlockData3}
                        });
                secondProposingTx.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
            }
            
            {
                var secondProposingTx =
                    await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(secondCrossChainBlockData);
                var secondProposalId = ProposalCreated.Parser.ParseFrom(secondProposingTx.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
                await ApproveWithMinersAsync(secondProposalId);
                await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(secondProposalId);
                var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
                indexedHeight.Value.ShouldBe(parentChainHeightOfCreation +
                                             secondCrossChainBlockData.ParentChainBlockDataList.Count);
            }
        }
        
        [Fact]
        public async Task Release_Indexing_WithoutPropose()
        {
            var parentChainId = 123;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            var organizationAddress =
                (await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty())).OwnerAddress;

            // create a normal proposal
            var proposalTx = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.RecordCrossChainData),
                OrganizationAddress = organizationAddress,
                ExpiredTime = TimestampHelper.GetUtcNow().AddMinutes(10),
                ToAddress = CrossChainContractAddress,
                Params = new RecordCrossChainDataInput
                {
                    ProposedCrossChainData = crossChainBlockData,
                    Proposer = DefaultSender
                }.ToByteString()
            });
            proposalTx.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposalTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();
            
            // approve
            await ApproveWithMinersAsync(proposalId);

            // release
            var releaseTx = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            releaseTx.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseTx.TransactionResult.Error.ShouldContain("Incorrect cross chain indexing proposal status.");
        }
        
        [Fact]
        public async Task GetIndexedCrossChainBlockData()
        {
            var parentChainId = 123;
            var lockedToken = 2;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockData1, sideChainBlockData2}
            };
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            await ApproveWithMinersAsync(proposalId);
            
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexing.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var indexedCrossChainBlockData =
                await CrossChainContractStub.GetIndexedCrossChainBlockDataByHeight.CallAsync(new SInt64Value
                    {Value = releaseResult.TransactionResult.BlockNumber});
            
            indexedCrossChainBlockData.SideChainBlockDataList.ShouldBe(crossChainBlockData.SideChainBlockDataList);
            indexedCrossChainBlockData.ParentChainBlockDataList.ShouldBeEmpty();
            
            var indexedSideChainBlockData =
                await CrossChainContractStub.GetIndexedSideChainBlockDataByHeight.CallAsync(new SInt64Value
                    {Value = releaseResult.TransactionResult.BlockNumber});
            
            indexedSideChainBlockData.SideChainBlockDataList.ShouldBe(crossChainBlockData.SideChainBlockDataList);
        }
        
        #endregion
        
        #region Verification

        [Fact]
        public async Task CrossChain_MerklePath()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var transactionId = Hash.FromString("sideChainBlockHash");
            
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] {transactionId, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.Root;
            var merklePath = binaryMerkleTree.GenerateMerklePath(0);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);

            long sideChainHeight = 1;
            parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            await DoIndexAsync(crossChainBlockData);

            var crossChainMerkleProofContext =
                await CrossChainContractStub.GetBoundParentChainHeightAndMerklePathByHeight.CallAsync(new SInt64Value
                    {Value = sideChainHeight});
            Assert.Equal(merklePath.ToByteString(),
                crossChainMerkleProofContext.MerklePathFromParentChain.ToByteString());
            var calculatedRoot = crossChainMerkleProofContext.MerklePathFromParentChain
                .ComputeRootWithLeafNode(transactionId);
            Assert.Equal(merkleTreeRoot, calculatedRoot);
        }

        [Fact]
        public async Task CrossChain_Verification()
        {
            int parentChainId = 123;
            long lockedToken = 10;
            long parentChainHeightOfCreation = 10;
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
            var txId = Hash.FromString("sideChainBlockHash");
            
            var fakeHash1 = Hash.FromString("fake1");
            var fakeHash2 = Hash.FromString("fake2");

            var rawBytes = txId.ToByteArray()
                .Concat(EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString()))
                .ToArray();
            var hash = Hash.FromRawBytes(rawBytes);

            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] {hash, fakeHash1, fakeHash2});
            var merkleTreeRoot = binaryMerkleTree.Root;
            var merklePath = binaryMerkleTree.GenerateMerklePath(0);
            Hash fakeTransactionStatusMerkleRoot = Hash.FromString("TransactionStatusMerkleRoot");
            var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
                fakeTransactionStatusMerkleRoot);
            parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
            {
                TransactionStatusMerkleTreeRoot = merkleTreeRoot
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData}
            };

            await DoIndexAsync(crossChainBlockData);

            var verificationInput = new VerifyTransactionInput()
            {
                TransactionId = txId,
                ParentChainHeight = parentChainHeightOfCreation,
                Path = merklePath
            };

            var txRes = await CrossChainContractStub.VerifyTransaction.SendAsync(verificationInput);
            var verified = BoolValue.Parser.ParseFrom(txRes.TransactionResult.ReturnValue).Value;
            Assert.True(verified);
        }

        #endregion

        [Fact]
        public async Task LockedToken_Verification()
        {
            var parentChainId = 123;
            var lockedToken = 100_000L;
            long parentChainHeightOfCreation = 10;
            var sideChainId =
                await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

            var lockedToken1 =
                await CrossChainContractStub.GetSideChainBalance.CallAsync(new SInt32Value {Value = sideChainId});
            lockedToken1.Value.ShouldBe(lockedToken);

            var address = await CrossChainContractStub.GetSideChainCreator.CallAsync(new SInt32Value {Value = sideChainId});
            address.ShouldBe(Address.FromPublicKey(DefaultKeyPair.PublicKey));
        }
        
        #region HelpMethods

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