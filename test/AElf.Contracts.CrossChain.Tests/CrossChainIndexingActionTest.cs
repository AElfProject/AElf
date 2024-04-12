using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.CSharp.Core.Utils;
using AElf.Kernel;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.CrossChain.Tests;

public class CrossChainIndexingActionTest : CrossChainContractTestBase<CrossChainContractTestAElfModule>
{
    [Fact]
    public async Task LockedToken_Verification()
    {
        var parentChainId = 123;
        var lockedToken = 100_000L;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

        var lockedToken1 =
            await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value { Value = sideChainId });
        lockedToken1.Value.ShouldBe(lockedToken);

        var address =
            await CrossChainContractStub.GetSideChainCreator.CallAsync(new Int32Value { Value = sideChainId });
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

    #region Propose

    [Fact]
    public async Task ProposeCrossChainData_Twice()
    {
        var parentChainId = 123;
        var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData }
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
        var parentChainId = 123;
        var sideChainId = await InitAndCreateSideChainAsync(parentChainId);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData }
        };

        {
            var txRes = await GetCrossChainContractStub(Accounts.Last().KeyPair).ProposeCrossChainIndexing
                .SendWithExceptionAsync(crossChainBlockData);
            Assert.Equal(TransactionResultStatus.Failed, txRes.TransactionResult.Status);
        }
    }

    //        #region Parent chain

    [Fact]
    public async Task ProposeCrossChainData_ParentChainBlockData()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

        var fakeTransactionStatusMerkleRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot");
        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
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
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            var pendingProposal = pendingProposalStatus.ChainIndexingProposalStatus[parentChainId];
            Assert.Equal(proposalId, pendingProposal.ProposalId);
            Assert.Equal(DefaultSender, pendingProposal.Proposer);
            Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
            Assert.False(pendingProposal.ToBeReleased);
        }
    }

    [Fact]
    public async Task ProposeCrossChainData_WrongParentChainId()
    {
        var parentChainId = 123;
        await InitAndCreateSideChainAsync(parentChainId);
        var fakeParentChainId = 124;
        var parentChainBlockData = CreateParentChainBlockData(1, fakeParentChainId, null);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
        };

        var txRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
        txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
    }

    [Fact]
    public async Task ProposeParentChainData_WrongHeight()
    {
        var parentChainId = 123;
        await InitAndCreateSideChainAsync(parentChainId);
        var parentChainBlockData = CreateParentChainBlockData(0, parentChainId, null);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
        };

        var txRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
        Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
    }

    [Fact]
    public async Task ProposeParentChainData_ContinuousData()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

        var fakeTransactionStatusMerkleRoot1 = HashHelper.ComputeFrom("TransactionStatusMerkleRoot1");
        var parentChainBlockData1 = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot1);

        var fakeTransactionStatusMerkleRoot2 = HashHelper.ComputeFrom("TransactionStatusMerkleRoot2");
        var parentChainBlockData2 = CreateParentChainBlockData(parentChainHeightOfCreation + 1, parentChainId,
            fakeTransactionStatusMerkleRoot2);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData1, parentChainBlockData2 }
        };

        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        Assert.Equal(TransactionResultStatus.Mined, txRes.TransactionResult.Status);
    }

    [Fact]
    public async Task RecordParentChainData_DiscontinuousData()
    {
        var parentChainId = 123;
        await InitAndCreateSideChainAsync(parentChainId);
        var parentChainBlockData1 = CreateParentChainBlockData(1, parentChainId, null);

        var parentChainBlockData2 = CreateParentChainBlockData(3, parentChainId, null);

        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData1, parentChainBlockData2 }
        };

        var txRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
        Assert.True(txRes.TransactionResult.Status == TransactionResultStatus.Failed);
    }

    [Fact]
    public async Task GetParentChainHeight_WithoutIndexing()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

        var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
        Assert.Equal(parentChainHeightOfCreation - 1, height.Value);
    }

    [Fact]
    public async Task GetParentChainHeight()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

        {
            var height = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
            Assert.Equal(parentChainHeightOfCreation - 1, height.Value);
        }
        var fakeTransactionStatusMerkleRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot");
        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
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

        var rechargeInput = new RechargeInput
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

        var proposalId = await DisposeSideChainProposalAsync(new Int32Value { Value = sideChainId });
        await ApproveWithMinersAsync(proposalId);
        await ReleaseProposalAsync(proposalId);

        var rechargeInput = new RechargeInput
        {
            ChainId = sideChainId,
            Amount = 100_000L
        };

        var chainStatus =
            await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value { Value = sideChainId });
        Assert.True(chainStatus.Status == SideChainStatus.Terminated);

        var txResult = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult.TransactionResult.Error.Contains("Side chain not found or incorrect side chain status.")
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
        var rechargeInput = new RechargeInput
        {
            ChainId = otherChainId,
            Amount = 100_000L
        };
        await ApproveBalanceAsync(100_000L);
        var txResult = await CrossChainContractStub.Recharge.SendWithExceptionAsync(rechargeInput);
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult.TransactionResult.Error.Contains("Side chain not found or incorrect side chain status.")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task RechargeForSideChain_IndexingFeeDebt()
    {
        var parentChainId = 123;
        long lockedToken = 2;
        long indexingPrice = 1;
        long parentChainHeightOfCreation = 10;

        // transfer token
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 1000,
            Symbol = "ELF",
            To = AnotherSender
        });

        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken,
                indexingPrice, AnotherKeyPair);

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");

        {
            var sideChainBlockData1 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData2 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
            };

            await DoIndexAsync(crossChainBlockData, new[] { sideChainId });
            var chainStatus = await GetSideChainStatusAsync(sideChainId);
            chainStatus.ShouldBe(SideChainStatus.Active);
        }

        {
            await ApproveBalanceAsync(1);
            var rechargeInput = new RechargeInput
            {
                ChainId = sideChainId,
                Amount = 1
            };
            await CrossChainContractStub.Recharge.SendAsync(rechargeInput);
            var chainStatus = await GetSideChainStatusAsync(sideChainId);
            chainStatus.ShouldBe(SideChainStatus.Active);
            var balance = await GetSideChainBalanceAsync(sideChainId);
            balance.ShouldBe(1);
        }

        {
            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);
            var sideChainBlockData4 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 4, sideChainId, fakeTxMerkleTreeRoot);
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = { sideChainBlockData3, sideChainBlockData4 }
            };

            await DoIndexAsync(crossChainBlockData, new[] { sideChainId });
            var chainStatus = await GetSideChainStatusAsync(sideChainId);
            chainStatus.ShouldBe(SideChainStatus.IndexingFeeDebt);

            (await CrossChainContractStub.GetSideChainIndexingFeeDebt.CallWithExceptionAsync(new Int32Value
                { Value = 0 })).Value.ShouldContain("Side chain not found.");

            var debt = await CrossChainContractStub.GetSideChainIndexingFeeDebt.CallAsync(new Int32Value
                { Value = sideChainId });
            debt.Value.ShouldBe(1);
        }

        {
            await ApproveBalanceAsync(2);
            var rechargeInput = new RechargeInput
            {
                ChainId = sideChainId,
                Amount = 2
            };
            var balanceBeforeRecharge = await GetSideChainBalanceAsync(sideChainId);
            balanceBeforeRecharge.ShouldBe(0);

            var rechargeTx = await CrossChainContractStub.Recharge.SendAsync(rechargeInput);

            var transferredNonIndexedEvents = rechargeTx.TransactionResult.Logs
                .Where(l => l.Name.Contains(nameof(Transferred))).Select(e => e.NonIndexed);
            var transferredNonIndexed = Transferred.Parser.ParseFrom(transferredNonIndexedEvents.Last());
            transferredNonIndexed.Amount.ShouldBe(1);

            var transferredIndexedEvents = rechargeTx.TransactionResult.Logs
                .Where(l => l.Name.Contains(nameof(Transferred))).Select(e => e.Indexed[1]);
            var transferredIndexed = Transferred.Parser.ParseFrom(transferredIndexedEvents.Last());
            transferredIndexed.To.ShouldBe(DefaultSender);

            var balanceAfterRecharge = await GetSideChainBalanceAsync(sideChainId);
            balanceAfterRecharge.ShouldBe(1);
            var chainStatus = await GetSideChainStatusAsync(sideChainId);
            chainStatus.ShouldBe(SideChainStatus.Active);

            var debt = await CrossChainContractStub.GetSideChainIndexingFeeDebt.CallAsync(new Int32Value
                { Value = sideChainId });
            debt.Value.ShouldBe(0);
        }
    }

    [Fact]
    public async Task ProposeSideChainData()
    {
        var parentChainId = 123;
        var lockedToken = 2;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
        };
        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        Assert.NotNull(proposalId);
        var crossChainIndexingDataProposedEvent = CrossChainIndexingDataProposedEvent.Parser
            .ParseFrom(txRes.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(CrossChainIndexingDataProposedEvent))).NonIndexed);
        Assert.Equal(crossChainBlockData, crossChainIndexingDataProposedEvent.ProposedCrossChainData);

        var pendingProposal =
            await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
        var chainIndexingProposalStatus = pendingProposal.ChainIndexingProposalStatus[sideChainId];
        crossChainIndexingDataProposedEvent.ProposalId.ShouldBe(proposalId);
        chainIndexingProposalStatus.Proposer.ShouldBe(DefaultSender);
        chainIndexingProposalStatus.ProposedCrossChainBlockData.ShouldBe(crossChainBlockData);
        chainIndexingProposalStatus.ToBeReleased.ShouldBeFalse();
    }

    [Fact]
    public async Task ProposeSideChainData_MultiTimesInOneBlock()
    {
        var parentChainId = 123;
        var lockedToken = 2;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

        var firstCrossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1 }
        };

        var secondCrossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData2 }
        };

        var tx1 = CrossChainContractStub.ProposeCrossChainIndexing.GetTransaction(firstCrossChainBlockData);
        var tx2 = CrossChainContractStub.ProposeCrossChainIndexing.GetTransaction(secondCrossChainBlockData);
        var blockExecutedSet = await MineAsync(new List<Transaction> { tx1, tx2 });
        blockExecutedSet.TransactionResultMap[tx2.GetHash()].Status.ShouldBe(TransactionResultStatus.Failed);
        blockExecutedSet.TransactionResultMap[tx2.GetHash()].Error.ShouldContain("Cannot execute this tx.");
    }

    [Fact]
    public async Task ProposeSideChainData_EmptyInput()
    {
        var parentChainId = 123;
        var lockedToken = 2;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

        var crossChainBlockData = new CrossChainBlockData();
        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
        txRes.TransactionResult.Error.ShouldContain("Empty cross chain data proposed.");
    }

    [Fact]
    public async Task ProposeSideChainData_WithChainNotExist()
    {
        var parentChainId = 123;
        long lockedToken = 10;
        long parentChainHeightOfCreation = 10;
        var sideChainId1 =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

        // create second side chain
        long lockedTokenAmount = 10;
        await ApproveBalanceAsync(lockedTokenAmount);

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId1, fakeTxMerkleTreeRoot);
        var fakeChainId = 124;

        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, fakeChainId, fakeTxMerkleTreeRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
        };

        var txRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
        txRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed");
    }

    [Fact]
    public async Task ProposeSideChainData_WithChainIndexingFeeDebt()
    {
        var parentChainId = 123;
        long lockedToken = 2;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData3 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2, sideChainBlockData3 }
        };

        var txResult =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);

        var proposalId = ProposalCreated.Parser
            .ParseFrom(txResult.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;
        await ApproveWithMinersAsync(proposalId);

        // release
        await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });

        var debt = await CrossChainContractStub.GetSideChainIndexingFeeDebt.CallAsync(new Int32Value
            { Value = sideChainId });
        debt.Value.ShouldBe(1);
    }

    [Fact]
    public async Task ProposeSideChainData_Inconsistent()
    {
        var parentChainId = 123;
        long lockedToken = 5;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData3 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 4, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2, sideChainBlockData3 }
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
            await CrossChainContractStub.GetChainInitializationData.CallWithExceptionAsync(new Int32Value
                { Value = parentChainId });
        error.Value.ShouldContain("Side chain not found.");

        //valid chain id
        var chainInitializationContext =
            await CrossChainContractStub.GetChainInitializationData.CallAsync(new Int32Value
                { Value = sideChainId });
        chainInitializationContext.ChainId.ShouldBe(sideChainId);
        chainInitializationContext.Creator.ShouldBe(Address.FromPublicKey(DefaultKeyPair.PublicKey));
    }

    #endregion

    #region Release

    [Fact]
    public async Task Release_IndexingSideChain_Success()
    {
        var lockedToken = 5;
        var sideChainId =
            await InitAndCreateSideChainAsync(0, 0, lockedToken);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
        };
        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        Assert.NotNull(proposalId);

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            var pendingProposal = pendingProposalStatus.ChainIndexingProposalStatus[sideChainId];
            Assert.Equal(proposalId, pendingProposal.ProposalId);
            Assert.Equal(DefaultSender, pendingProposal.Proposer);
            Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
            Assert.False(pendingProposal.ToBeReleased);
        }

        await ApproveWithMinersAsync(proposalId);

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            var pendingProposal = pendingProposalStatus.ChainIndexingProposalStatus[sideChainId];
            Assert.Equal(proposalId, pendingProposal.ProposalId);
            Assert.Equal(DefaultSender, pendingProposal.Proposer);
            Assert.Equal(crossChainBlockData, pendingProposal.ProposedCrossChainBlockData);
            Assert.True(pendingProposal.ToBeReleased);
        }

        {
            var error = await CrossChainContractStub.GetSideChainHeight.CallWithExceptionAsync(new Int32Value
            {
                Value = 0
            });
            error.Value.ShouldContain("Side chain not found.");
        }

        {
            var height = await CrossChainContractStub.GetParentChainHeight.CallWithExceptionAsync(new Empty());

            var error = await CrossChainContractStub.GetParentChainId.CallWithExceptionAsync(new Empty());
        }

        {
            var indexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            indexedHeight.Value.ShouldBe(0);

            var balance = await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            balance.Value.ShouldBe(lockedToken);
        }

        {
            var tx =
                await GetCrossChainContractStub(AnotherKeyPair).ReleaseCrossChainIndexingProposal
                    .SendWithExceptionAsync(
                        new ReleaseCrossChainIndexingProposalInput
                        {
                            ChainIdList = { sideChainId }
                        });
            tx.TransactionResult.Error.ShouldContain("No permission.");
        }

        var result = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });

        {
            var indexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            indexedHeight.Value.ShouldBe(2);

            var logEvent = result.TransactionResult.Logs.First(l => l.Name == nameof(SideChainIndexed));
            var indexEvent =new SideChainIndexed();
            indexEvent.MergeFrom(logEvent.Indexed[0]);
            indexEvent.MergeFrom(logEvent.NonIndexed);
            indexEvent.ChainId.ShouldBe(sideChainId);
            indexEvent.IndexedHeight.ShouldBe(2);

            var balance = await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            balance.Value.ShouldBe(lockedToken - 2);

        }

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            pendingProposalStatus.ChainIndexingProposalStatus.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Release_IndexingMultiSideChains_Success()
    {
        var parentChainId = 123;
        var lockedToken = 5;
        long parentChainHeightOfCreation = 10;
        var firstChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var secondChainId =
            await CreateSideChainByDefaultSenderAsync(false, parentChainHeightOfCreation, parentChainId, lockedToken);

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var firstSideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, firstChainId, fakeTxMerkleTreeRoot);
        var firstSideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, firstChainId, fakeTxMerkleTreeRoot);

        var secondSideChainBlockData =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, secondChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { firstSideChainBlockData1, firstSideChainBlockData2, secondSideChainBlockData }
        };

        var firstProposingTxRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        var proposalCreationEvents =
            firstProposingTxRes.TransactionResult.Logs.Where(l => l.Name.Contains(nameof(ProposalCreated)))
                .Select(log => ProposalCreated.Parser.ParseFrom(log.NonIndexed)).ToList();

        proposalCreationEvents.Count.ShouldBe(2);

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());

            var pendingProposalForSideChainOne = pendingProposalStatus.ChainIndexingProposalStatus[firstChainId];
            AssertChainIndexingProposalStatus(pendingProposalForSideChainOne, DefaultSender,
                proposalCreationEvents[0].ProposalId, new CrossChainBlockData
                {
                    SideChainBlockDataList = { firstSideChainBlockData1, firstSideChainBlockData2 }
                }, false);

            var pendingProposalForSideChainTwo = pendingProposalStatus.ChainIndexingProposalStatus[secondChainId];
            AssertChainIndexingProposalStatus(pendingProposalForSideChainTwo, DefaultSender,
                proposalCreationEvents[1].ProposalId, new CrossChainBlockData
                {
                    SideChainBlockDataList = { secondSideChainBlockData }
                }, false);
        }

        await ApproveWithMinersAsync(proposalCreationEvents[0].ProposalId);

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            var pendingProposal = pendingProposalStatus.ChainIndexingProposalStatus[firstChainId];
            AssertChainIndexingProposalStatus(pendingProposal, DefaultSender,
                proposalCreationEvents[0].ProposalId, new CrossChainBlockData
                {
                    SideChainBlockDataList = { firstSideChainBlockData1, firstSideChainBlockData2 }
                }, true);
        }

        {
            var txResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendWithExceptionAsync(
                new ReleaseCrossChainIndexingProposalInput
                {
                    ChainIdList = { firstChainId, secondChainId }
                });
            txResult.TransactionResult.Error.ShouldContain("Not approved cross chain indexing proposal.");
        }

        await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { firstChainId }
            });

        {
            var indexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = firstChainId
            });
            indexedHeight.Value.ShouldBe(2);

            var balance = await CrossChainContractStub.GetSideChainBalance.CallAsync(new Int32Value
            {
                Value = firstChainId
            });
            balance.Value.ShouldBe(lockedToken - 2);
        }

        var firstSideChainBlockData3 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 3, firstChainId, fakeTxMerkleTreeRoot);
        var firstSideChainBlockData4 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 4, firstChainId, fakeTxMerkleTreeRoot);

        var secondProposingTxRes =
            await CrossChainContractStub.ProposeCrossChainIndexing
                .SendWithExceptionAsync(new CrossChainBlockData
                {
                    SideChainBlockDataList =
                        { firstSideChainBlockData3, firstSideChainBlockData4, secondSideChainBlockData }
                });
        secondProposingTxRes.TransactionResult.Error.ShouldContain("Chain indexing already proposed.");

        var secondCrossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { firstSideChainBlockData3, firstSideChainBlockData4 }
        };

        var thirdProposingTxRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(secondCrossChainBlockData);
        var secondProposalCreationEvents =
            thirdProposingTxRes.TransactionResult.Logs.Where(l => l.Name.Contains(nameof(ProposalCreated)))
                .Select(log => ProposalCreated.Parser.ParseFrom(log.NonIndexed)).ToList();

        secondProposalCreationEvents.Count.ShouldBe(1);

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());

            var pendingProposalForSideChainOne = pendingProposalStatus.ChainIndexingProposalStatus[firstChainId];
            AssertChainIndexingProposalStatus(pendingProposalForSideChainOne, DefaultSender,
                secondProposalCreationEvents[0].ProposalId, new CrossChainBlockData
                {
                    SideChainBlockDataList = { firstSideChainBlockData3, firstSideChainBlockData4 }
                }, false);

            var pendingProposalForSideChainTwo = pendingProposalStatus.ChainIndexingProposalStatus[secondChainId];
            AssertChainIndexingProposalStatus(pendingProposalForSideChainTwo, DefaultSender,
                proposalCreationEvents[1].ProposalId, new CrossChainBlockData
                {
                    SideChainBlockDataList = { secondSideChainBlockData }
                }, false);
        }

        await ApproveWithMinersAsync(proposalCreationEvents[1].ProposalId);
        await ApproveWithMinersAsync(secondProposalCreationEvents[0].ProposalId);

        var releaseTx1 = CrossChainContractStub.ReleaseCrossChainIndexingProposal.GetTransaction(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { firstChainId }
            });

        var releaseTx2 = CrossChainContractStub.ReleaseCrossChainIndexingProposal.GetTransaction(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { secondChainId }
            });

        var blockExecutedSet = await MineAsync(new List<Transaction> { releaseTx1, releaseTx2 });
        blockExecutedSet.TransactionResultMap[releaseTx2.GetHash()].Error.ShouldContain("Cannot execute this tx.");

        await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { secondChainId }
            });

        {
            var pendingProposalStatus =
                await CrossChainContractStub.GetIndexingProposalStatus.CallAsync(new Empty());
            pendingProposalStatus.ChainIndexingProposalStatus.ShouldBeEmpty();
        }

        {
            var firstSideChainIndexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = firstChainId
            });
            firstSideChainIndexedHeight.Value.ShouldBe(4);

            var secondSideChainIndexedHeight = await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = secondChainId
            });
            secondSideChainIndexedHeight.Value.ShouldBe(1);
        }

        var tx = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendWithExceptionAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { firstChainId, secondChainId }
            });
        tx.TransactionResult.Error.ShouldContain("Chain indexing not proposed.");
    }

    [Fact]
    public async Task Release_IndexingSideChain_ContinuousTwice()
    {
        var parentChainId = 123;
        var lockedToken = 5;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);


        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
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
            SideChainBlockDataList = { sideChainBlockData3, sideChainBlockData4, sideChainBlockData5 }
        };

        {
            var secondProposingTxRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(
                    secondCrossChainBlockData);
            secondProposingTxRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
        }

        {
            // empty input
            var releaseResult =
                await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendWithExceptionAsync(
                    new ReleaseCrossChainIndexingProposalInput());
            releaseResult.TransactionResult.Error.ShouldContain("Empty input not allowed.");
        }

        {
            var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput
                {
                    ChainIdList = { sideChainId }
                });
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var secondProposingTxRes =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(new CrossChainBlockData
                {
                    SideChainBlockDataList = { sideChainBlockData6 }
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
            await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput
                {
                    ChainIdList = { sideChainId }
                });
            var indexedHeight =
                await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value { Value = sideChainId });
            indexedHeight.Value.ShouldBe(5);
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            chainStatus.Status.ShouldBe(SideChainStatus.Active);
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
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
        };

        {
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
            txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();
            await ApproveWithMinersAsync(proposalId);
        }

        var disposeSideChainProposalId = await DisposeSideChainProposalAsync(new Int32Value { Value = sideChainId });
        await ApproveWithMinersAsync(disposeSideChainProposalId);
        await ReleaseProposalAsync(disposeSideChainProposalId);

        {
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            chainStatus.Status.ShouldBe(SideChainStatus.Terminated);
        }

        var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendWithExceptionAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });
        releaseResult.TransactionResult.Error.ShouldContain("Chain indexing not proposed.");

        {
            var sideChainBlockData3 =
                CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);
            var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(
                new CrossChainBlockData
                {
                    SideChainBlockDataList = { sideChainBlockData3 }
                });
            txRes.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed");
        }
    }

    [Fact]
    public async Task Release_IndexingSideChain_IndexingFeeDebt()
    {
        var parentChainId = 123;
        long lockedToken = 2;
        long indexingPrice = 1;
        long parentChainHeightOfCreation = 10;

        // transfer token
        var transferTx = await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 1000,
            Symbol = "ELF",
            To = AnotherSender
        });

        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken,
                indexingPrice, AnotherKeyPair);

        var balanceBeforeIndexing = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultSender,
            Symbol = "ELF"
        });

        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData3 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 3, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2, sideChainBlockData3 }
        };

        var txRes =
            await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        Assert.NotNull(proposalId);
        await ApproveWithMinersAsync(proposalId);

        {
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            chainStatus.Status.ShouldBe(SideChainStatus.Active);
        }

        var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { sideChainId }
            });
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            chainStatus.Status.ShouldBe(SideChainStatus.IndexingFeeDebt);
        }

        var sideChainIndexedHeight =
            (await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value { Value = sideChainId }))
            .Value;
        sideChainIndexedHeight.ShouldBe(crossChainBlockData.SideChainBlockDataList.Last().Height);

        var balanceAfterIndexing = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultSender,
            Symbol = "ELF"
        });
        balanceAfterIndexing.Balance.ShouldBe(balanceBeforeIndexing.Balance + lockedToken);

        // recharge
        var arrearsAmount = crossChainBlockData.SideChainBlockDataList.Count - lockedToken;
        var rechargeAmount = arrearsAmount + indexingPrice;
        // approve allowance
        await ApproveBalanceAsync(rechargeAmount, AnotherKeyPair);

        var crossChainContractStub = GetCrossChainContractStub(AnotherKeyPair);

        {
            var rechargeTxFailed = await crossChainContractStub.Recharge.SendWithExceptionAsync(new RechargeInput
            {
                ChainId = sideChainId,
                Amount = rechargeAmount - 1
            });
            rechargeTxFailed.TransactionResult.Error.ShouldContain("Indexing fee recharging not enough.");
        }

        var rechargeTx = await crossChainContractStub.Recharge.SendAsync(new RechargeInput
        {
            ChainId = sideChainId,
            Amount = rechargeAmount
        });
        rechargeTx.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var balanceAfterRecharge = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultSender,
            Symbol = "ELF"
        });
        balanceAfterRecharge.Balance.ShouldBe(balanceAfterIndexing.Balance + arrearsAmount);

        {
            var chainStatus = await CrossChainContractStub.GetChainStatus.CallAsync(new Int32Value
            {
                Value = sideChainId
            });
            chainStatus.Status.ShouldBe(SideChainStatus.Active);
        }
    }

    [Fact]
    public async Task Release_IndexingParentChain_Success()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
        var fakeTransactionStatusMerkleRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot");
        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
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

        var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { parentChainId }
            });
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
            indexedHeight.Value.ShouldBe(parentChainHeightOfCreation);
            
            var logEvent = releaseResult.TransactionResult.Logs.First(l => l.Name == nameof(ParentChainIndexed));
            var indexEvent =new ParentChainIndexed();
            indexEvent.MergeFrom(logEvent.Indexed[0]);
            indexEvent.MergeFrom(logEvent.NonIndexed);
            indexEvent.ChainId.ShouldBe(parentChainId);
            indexEvent.IndexedHeight.ShouldBe(indexedHeight.Value);
        }
    }

    [Fact]
    public async Task Release_IndexingParentChain_ContinuousTwice()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);
        var fakeTransactionStatusMerkleRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot");
        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
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
            ParentChainBlockDataList = { parentChainBlockData2, parentChainBlockData3 }
        };

        {
            var secondProposingTx =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(crossChainBlockData);
            secondProposingTx.TransactionResult.Error.ShouldContain("Chain indexing already proposed.");
        }

        {
            var secondProposingTx =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendWithExceptionAsync(
                    secondCrossChainBlockData);
            secondProposingTx.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
        }

        var releaseResult = await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
            new ReleaseCrossChainIndexingProposalInput
            {
                ChainIdList = { parentChainId }
            });
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
                        ParentChainBlockDataList = { parentChainBlockData3 }
                    });
            secondProposingTx.TransactionResult.Error.ShouldContain("Invalid cross chain data to be indexed.");
        }

        {
            var secondProposingTx =
                await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(secondCrossChainBlockData);
            var secondProposalId = ProposalCreated.Parser.ParseFrom(secondProposingTx.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            await ApproveWithMinersAsync(secondProposalId);
            await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput
                {
                    ChainIdList = { parentChainId }
                });
            var indexedHeight = await CrossChainContractStub.GetParentChainHeight.CallAsync(new Empty());
            indexedHeight.Value.ShouldBe(parentChainHeightOfCreation +
                                         secondCrossChainBlockData.ParentChainBlockDataList.Count);
        }
    }

    [Fact]
    public async Task AcceptCrossChainIndexingProposal_Failed()
    {
        var parentChainId = 123;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId);

        var organizationAddress =
            (await CrossChainContractStub.GetCrossChainIndexingController.CallAsync(new Empty())).OwnerAddress;

        // create a normal proposal
        {
            var proposalTx = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(CrossChainContractStub.AcceptCrossChainIndexingProposal),
                OrganizationAddress = organizationAddress,
                ExpiredTime = TimestampHelper.GetUtcNow().AddMinutes(10),
                ToAddress = CrossChainContractAddress,
                Params = new AcceptCrossChainIndexingProposalInput
                {
                    ChainId = parentChainId
                }.ToByteString()
            });
            proposalTx.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposalTx.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();

            // approve
            await ApproveWithMinersAsync(proposalId);

            // release
            var releaseTx = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            releaseTx.TransactionResult.Error.ShouldContain("Incorrect cross chain indexing proposal status.");
        }

        {
            // not authorized
            var tx = await CrossChainContractStub.AcceptCrossChainIndexingProposal.SendWithExceptionAsync(
                new AcceptCrossChainIndexingProposalInput
                {
                    ChainId = parentChainId
                });

            tx.TransactionResult.Error.ShouldContain("Unauthorized behavior.");
        }
    }

    [Fact]
    public async Task GetIndexedCrossChainBlockData_Test()
    {
        var parentChainId = 123;
        var lockedToken = 2;
        long parentChainHeightOfCreation = 10;
        var sideChainId =
            await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var fakeSideChainBlockHash = HashHelper.ComputeFrom("sideChainBlockHash");
        var fakeTxMerkleTreeRoot = HashHelper.ComputeFrom("txMerkleTreeRoot");
        var sideChainBlockData1 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 1, sideChainId, fakeTxMerkleTreeRoot);
        var sideChainBlockData2 =
            CreateSideChainBlockData(fakeSideChainBlockHash, 2, sideChainId, fakeTxMerkleTreeRoot);

        var crossChainBlockData = new CrossChainBlockData
        {
            SideChainBlockDataList = { sideChainBlockData1, sideChainBlockData2 }
        };
        var txRes = await CrossChainContractStub.ProposeCrossChainIndexing.SendAsync(crossChainBlockData);
        txRes.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalId = ProposalCreated.Parser
            .ParseFrom(txRes.TransactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        await ApproveWithMinersAsync(proposalId);

        var releaseResult =
            await CrossChainContractStub.ReleaseCrossChainIndexingProposal.SendAsync(
                new ReleaseCrossChainIndexingProposalInput { ChainIdList = { sideChainId } });
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var indexedCrossChainBlockData =
            await CrossChainContractStub.GetIndexedSideChainBlockDataByHeight.CallAsync(new Int64Value
                { Value = releaseResult.TransactionResult.BlockNumber });

        indexedCrossChainBlockData.SideChainBlockDataList.ShouldBe(crossChainBlockData.SideChainBlockDataList);
    }

    #endregion

    #region Verification

    [Fact]
    public async Task CrossChain_MerklePath_Test()
    {
        var parentChainId = 123;
        long lockedToken = 10;
        long parentChainHeightOfCreation = 10;
        await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var transactionId = HashHelper.ComputeFrom("sideChainBlockHash");

        var fakeHash1 = HashHelper.ComputeFrom("fake1");
        var fakeHash2 = HashHelper.ComputeFrom("fake2");

        var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] { transactionId, fakeHash1, fakeHash2 });
        var merkleTreeRoot = binaryMerkleTree.Root;
        var merklePath = binaryMerkleTree.GenerateMerklePath(0);
        var fakeTransactionStatusMerkleRoot = HashHelper.ComputeFrom("TransactionStatusMerkleRoot");
        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            fakeTransactionStatusMerkleRoot);

        long sideChainHeight = 1;
        parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData }
        };

        await DoIndexAsync(crossChainBlockData, new[] { parentChainId });

        {
            var crossChainMerkleProofContext =
                await CrossChainContractStub.GetBoundParentChainHeightAndMerklePathByHeight.CallWithExceptionAsync(
                    new Int64Value
                        { Value = sideChainHeight + 1 });
        }

        {
            var crossChainMerkleProofContext =
                await CrossChainContractStub.GetBoundParentChainHeightAndMerklePathByHeight.CallAsync(new Int64Value
                    { Value = sideChainHeight });
            Assert.Equal(merklePath.ToByteString(),
                crossChainMerkleProofContext.MerklePathFromParentChain.ToByteString());
            var calculatedRoot = crossChainMerkleProofContext.MerklePathFromParentChain
                .ComputeRootWithLeafNode(transactionId);
            Assert.Equal(merkleTreeRoot, calculatedRoot);
        }
    }

    [Fact]
    public async Task CrossChain_Verification()
    {
        var parentChainId = 123;
        long lockedToken = 10;
        long parentChainHeightOfCreation = 10;
        var sideChainId = await InitAndCreateSideChainAsync(parentChainHeightOfCreation, parentChainId, lockedToken);
        var txId = HashHelper.ComputeFrom("sideChainBlockHash");

        var fakeHash1 = HashHelper.ComputeFrom("fake1");
        var fakeHash2 = HashHelper.ComputeFrom("fake2");

        var rawBytes = ByteArrayHelper.ConcatArrays(txId.ToByteArray(),
            EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString()));
        var hash = HashHelper.ComputeFrom(rawBytes);

        var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] { hash, fakeHash1, fakeHash2 });
        var merkleTreeRoot = binaryMerkleTree.Root;

        var parentChainTxId = HashHelper.ComputeFrom("parentChainTx");
        var parentChainTxStatusRawBytes = ByteArrayHelper.ConcatArrays(parentChainTxId.ToByteArray(),
            EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString()));
        var parentChainTxStatusMerkleTree = BinaryMerkleTree.FromLeafNodes(new[]
        {
            fakeHash1,
            HashHelper.ComputeFrom(parentChainTxStatusRawBytes),
            fakeHash2
        });

        var parentChainBlockData = CreateParentChainBlockData(parentChainHeightOfCreation, parentChainId,
            parentChainTxStatusMerkleTree.Root);
        var sideChainTxId = HashHelper.ComputeFrom("sideChainTx");
        var sideChainTxStatusRawBytes = ByteArrayHelper.ConcatArrays(sideChainTxId.ToByteArray(),
            EncodingHelper.EncodeUtf8(TransactionResultStatus.Mined.ToString()));
        var sideChainTxStatusMerkleTree = BinaryMerkleTree.FromLeafNodes(new[]
        {
            fakeHash1,
            fakeHash2,
            HashHelper.ComputeFrom(sideChainTxStatusRawBytes)
        });

        var sideChainBlockData = CreateSideChainBlockData(HashHelper.ComputeFrom("SideChainBlockHash"), 1,
            sideChainId, sideChainTxStatusMerkleTree.Root);

        parentChainBlockData.CrossChainExtraData = new CrossChainExtraData
        {
            TransactionStatusMerkleTreeRoot = merkleTreeRoot
        };
        var crossChainBlockData = new CrossChainBlockData
        {
            ParentChainBlockDataList = { parentChainBlockData },
            SideChainBlockDataList = { sideChainBlockData }
        };

        var blockHeight = await DoIndexAsync(crossChainBlockData, new[] { parentChainId, sideChainId });

        {
            var merklePath = binaryMerkleTree.GenerateMerklePath(0);

            // cousin chain verification
            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = txId,
                    ParentChainHeight = parentChainHeightOfCreation,
                    Path = merklePath
                };

                var txRes = await CrossChainContractStub.VerifyTransaction.SendAsync(verificationInput);
                var verified = BoolValue.Parser.ParseFrom(txRes.TransactionResult.ReturnValue).Value;
                verified.ShouldBeTrue();
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = txId,
                    ParentChainHeight = parentChainHeightOfCreation + 1,
                    Path = merklePath
                };

                var error = await CrossChainContractStub.VerifyTransaction
                    .CallWithExceptionAsync(verificationInput);
                error.Value.ShouldContain(
                    $"Parent chain block at height {verificationInput.ParentChainHeight} is not recorded.");
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = fakeHash1,
                    ParentChainHeight = parentChainHeightOfCreation,
                    Path = merklePath
                };

                var res = await CrossChainContractStub.VerifyTransaction.CallAsync(verificationInput);
                res.Value.ShouldBeFalse();
            }
        }

        {
            // parent chain verification

            var merklePath = parentChainTxStatusMerkleTree.GenerateMerklePath(1);

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = parentChainTxId,
                    ParentChainHeight = parentChainHeightOfCreation,
                    Path = merklePath,
                    VerifiedChainId = parentChainId
                };

                var txRes = await CrossChainContractStub.VerifyTransaction.SendAsync(verificationInput);
                var verified = BoolValue.Parser.ParseFrom(txRes.TransactionResult.ReturnValue).Value;
                verified.ShouldBeTrue();
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = parentChainTxId,
                    ParentChainHeight = parentChainHeightOfCreation + 1,
                    Path = merklePath,
                    VerifiedChainId = parentChainId
                };

                var error = await CrossChainContractStub.VerifyTransaction
                    .CallWithExceptionAsync(verificationInput);
                error.Value.ShouldContain(
                    $"Parent chain block at height {verificationInput.ParentChainHeight} is not recorded.");
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = fakeHash1,
                    ParentChainHeight = parentChainHeightOfCreation,
                    Path = merklePath,
                    VerifiedChainId = parentChainId
                };

                var res = await CrossChainContractStub.VerifyTransaction.CallAsync(verificationInput);
                res.Value.ShouldBeFalse();
            }
        }

        {
            // side chain verification
            var merklePath = sideChainTxStatusMerkleTree.GenerateMerklePath(2);
            merklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = sideChainTxStatusMerkleTree.Root,
                IsLeftChildNode = true
            });

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = sideChainTxId,
                    ParentChainHeight = blockHeight,
                    Path = merklePath,
                    VerifiedChainId = sideChainId
                };

                var txRes = await CrossChainContractStub.VerifyTransaction.SendAsync(verificationInput);
                var verified = BoolValue.Parser.ParseFrom(txRes.TransactionResult.ReturnValue).Value;
                verified.ShouldBeTrue();
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = sideChainTxId,
                    ParentChainHeight = blockHeight + 1,
                    Path = merklePath,
                    VerifiedChainId = sideChainId
                };

                var error = await CrossChainContractStub.VerifyTransaction
                    .CallWithExceptionAsync(verificationInput);
            }

            {
                var verificationInput = new VerifyTransactionInput
                {
                    TransactionId = fakeHash1,
                    ParentChainHeight = blockHeight,
                    Path = merklePath,
                    VerifiedChainId = sideChainId
                };

                var res = await CrossChainContractStub.VerifyTransaction.CallAsync(verificationInput);
                res.Value.ShouldBeFalse();
            }
        }
    }

    #endregion
}