using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Proposal.Tests.Application
{
    public class ProposalApprovalTransactionGeneratorTests : ProposalTestBase
    {
        private readonly ISystemTransactionGenerator _proposalApprovalTransactionGenerator;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly ProposalTestHelper _proposalTestHelper;
        
        public ProposalApprovalTransactionGeneratorTests()
        {
            _proposalApprovalTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
            _proposalTestHelper = GetRequiredService<ProposalTestHelper>();
        }
        
        [Fact]
        public async Task GenerateTransactionsAsync_Without_PackedTransaction_Test()
        {
            var address = NormalAddress;
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 10
            };
            var proposalId = HashHelper.ComputeFrom("proposal");
            _proposalTestHelper.AddNotVotedProposalIdList(new List<Hash>{proposalId});
            
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();
            proposalCacheProvider.AddProposal(proposalId, 5);
            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, false);
            var transactionList = await _proposalApprovalTransactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight, blockIndex.BlockHash);
            transactionList.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task GenerateTransactionsAsync_Without_NotApprovedProposal_Test()
        {
            var address = NormalAddress;
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 10
            };
            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, true);
            var transactionList = await _proposalApprovalTransactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight, blockIndex.BlockHash);
            transactionList.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GenerateTransactionsAsync_Success_Test()
        {
            var address = NormalAddress;
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 10
            };
            var proposalId = HashHelper.ComputeFrom("proposal");
            _proposalTestHelper.AddNotVotedProposalIdList(new List<Hash>{proposalId});
            
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();
            proposalCacheProvider.AddProposal(proposalId, 5);
            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, true);
            var transactionList = await _proposalApprovalTransactionGenerator.GenerateTransactionsAsync(address, blockIndex.BlockHeight, blockIndex.BlockHash);
            transactionList.Count.ShouldBe(1);
        }
    }
}