using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.CodeCheck.Tests;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.Txn.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckProposalReleaseTransactionGeneratorTests : CodeCheckTestBase
{
    private readonly ISystemTransactionGenerator _transactionGenerator;
    private readonly ProposalTestHelper _proposalTestHelper;
    private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
    private readonly IBlockStateSetManger _blockStateSetManger;
    private readonly ICodeCheckProposalProvider _codeCheckProposalProvider;

    public CodeCheckProposalReleaseTransactionGeneratorTests()
    {
        _transactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
        _transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
        _proposalTestHelper = GetRequiredService<ProposalTestHelper>();
        _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        _codeCheckProposalProvider = GetRequiredService<ICodeCheckProposalProvider>();
    }

    [Fact]
    public async Task GenerateTransactions_Without_PackedTransaction_Test()
    {
        var address = NormalAddress;
        var blockIndex = new BlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 10
        };
        var proposalId = HashHelper.ComputeFrom("proposal");
        var contractInputHash  = HashHelper.ComputeFrom("contractInputHash");
        _proposalTestHelper.AddReleaseThresholdReachedProposalIdList(new List<Hash> { proposalId });

        _codeCheckProposalProvider.AddProposal(proposalId, contractInputHash,5);
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = blockIndex.BlockHash,
            BlockHeight = blockIndex.BlockHeight
        });
        await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, false);
        var transactionList =
            await _transactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight,
                blockIndex.BlockHash);
        transactionList.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task GenerateTransactions_Without_ToReleased_Test()
    {
        var address = NormalAddress;
        var blockIndex = new BlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 10
        };
        var proposalId = HashHelper.ComputeFrom("proposal");
        var contractInputHash  = HashHelper.ComputeFrom("contractInputHash");
        _proposalTestHelper.AddReleaseThresholdReachedProposalIdList(new List<Hash> { proposalId });
        
        var transactionList =
            await _transactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight,
                blockIndex.BlockHash);
        transactionList.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task GenerateTransactions_Success_Test()
    {
        var address = NormalAddress;
        var blockIndex = new BlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 10
        };
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = blockIndex.BlockHash,
            BlockHeight = blockIndex.BlockHeight
        });
        
        var proposalId = HashHelper.ComputeFrom("proposal");
        var contractInputHash  = HashHelper.ComputeFrom("contractInputHash");
        _proposalTestHelper.AddReleaseThresholdReachedProposalIdList(new List<Hash> { proposalId });
        _codeCheckProposalProvider.AddProposal(proposalId, contractInputHash, 5);
        
        var transactionList =
            await _transactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight,
                blockIndex.BlockHash);
        transactionList.Count.ShouldBe(1);
        transactionList[0].From.ShouldBe(address);
        transactionList[0].MethodName.ShouldBe(nameof(ACS0Container.ACS0Stub.ReleaseApprovedUserSmartContract));
        transactionList[0].To.ShouldBe(ZeroContractFakeAddress);
        var @params = ReleaseContractInput.Parser.ParseFrom(transactionList[0].Params);
        @params.ProposalId.ShouldBe(proposalId);
        @params.ProposedContractInputHash.ShouldBe(contractInputHash);

        transactionList =
            await _transactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight,
                blockIndex.BlockHash);
        transactionList.Count.ShouldBe(0);
    }
}