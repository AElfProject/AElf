using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Proposal.Tests.Application
{
    public class ProposalServiceTests : ProposalTestBase
    {
        private readonly IProposalService _proposalService;
        private readonly ProposalTestHelper _proposalTestHelper;

        public ProposalServiceTests()
        {
            _proposalService = GetRequiredService<IProposalService>();
            _proposalTestHelper = GetRequiredService<ProposalTestHelper>();
        }

        [Fact]
        public void AddNotApprovedProposalTest()
        {
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();

            {
                var proposalId = HashHelper.ComputeFrom("proposal");
                const int height = 100;
                _proposalService.AddNotApprovedProposal(proposalId, height);
                var exist = proposalCacheProvider.TryGetProposalCreatedHeight(proposalId, out var h);
                exist.ShouldBeTrue();
                h.ShouldBe(height);
            }

            {
                var proposalId = HashHelper.ComputeFrom("proposal");
                const int height1 = 101;
                const int height2 = 100;
                _proposalService.AddNotApprovedProposal(proposalId, height1);
                _proposalService.AddNotApprovedProposal(proposalId, height2);
                var exist = proposalCacheProvider.TryGetProposalCreatedHeight(proposalId, out var h);
                exist.ShouldBeTrue();
                h.ShouldBe(height1);
            }
        }

        [Fact]
        public async Task GetNotApprovedProposalIdListTest()
        {
            var proposalId1 = HashHelper.ComputeFrom("proposalId1");
            var proposalId2 = HashHelper.ComputeFrom("proposalId2");
            var proposalId3 = HashHelper.ComputeFrom("proposalId3");
            var proposalId4 = HashHelper.ComputeFrom("proposalId4");
            
            var notApprovedProposalIdList = new List<Hash>
            {
                proposalId1, proposalId2, proposalId3, proposalId4
            };
            _proposalTestHelper.AddNotVotedProposalIdList(notApprovedProposalIdList);
            
            var blockHash = HashHelper.ComputeFrom("BlockHash");
            var blockHeight = 10;
            
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();
            proposalCacheProvider.AddProposal(proposalId1, 5);

            {
                var queryResultNotApprovedProposalIdList =
                    await _proposalService.GetNotApprovedProposalIdListAsync(NormalAddress, blockHash, blockHeight);
                queryResultNotApprovedProposalIdList.Count.ShouldBe(1);
                queryResultNotApprovedProposalIdList.ShouldContain(proposalId1);
            }
            
            proposalCacheProvider.AddProposal(proposalId2, 6);
            {
                var queryResultNotApprovedProposalIdList =
                    await _proposalService.GetNotApprovedProposalIdListAsync(NormalAddress, blockHash, blockHeight);
                queryResultNotApprovedProposalIdList.Count.ShouldBe(2);
                queryResultNotApprovedProposalIdList.ShouldContain(proposalId1);
                queryResultNotApprovedProposalIdList.ShouldContain(proposalId2);
            }
        }
        
        [Fact]
        public async Task GetNotApprovedProposalIdListTest_ReturnEmpty()
        {
            var proposalId1 = HashHelper.ComputeFrom("proposalId1");
            var proposalId2 = HashHelper.ComputeFrom("proposalId2");
            var proposalId3 = HashHelper.ComputeFrom("proposalId3");
            var proposalId4 = HashHelper.ComputeFrom("proposalId4");
            
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();
            proposalCacheProvider.AddProposal(proposalId1, 5);
            proposalCacheProvider.AddProposal(proposalId2, 5);

            var notApprovedProposalIdList = new List<Hash>
            {
                proposalId3, proposalId4
            };
            _proposalTestHelper.AddNotVotedProposalIdList(notApprovedProposalIdList);
            
            var blockHash = HashHelper.ComputeFrom("BlockHash");
            var blockHeight = 10;
            var queryResultNotApprovedProposalIdList =
                await _proposalService.GetNotApprovedProposalIdListAsync(NormalAddress, blockHash, blockHeight);
            queryResultNotApprovedProposalIdList.ShouldBeEmpty();
        }

        [Fact]
        public async Task ClearProposalTest()
        {
            var proposalId1 = HashHelper.ComputeFrom("proposalId1");
            var proposalId2 = HashHelper.ComputeFrom("proposalId2");
            var proposalId3 = HashHelper.ComputeFrom("proposalId3");
            var proposalId4 = HashHelper.ComputeFrom("proposalId4");
            
            var proposalCacheProvider = GetRequiredService<IProposalProvider>();
            var blockHeight = 5;

            proposalCacheProvider.AddProposal(proposalId1, blockHeight);
            proposalCacheProvider.AddProposal(proposalId2, blockHeight);
            proposalCacheProvider.AddProposal(proposalId3, blockHeight);
            proposalCacheProvider.AddProposal(proposalId4, blockHeight);
            
            var notApprovedProposalIdList = new List<Hash>
            {
                proposalId3, proposalId4
            };
            _proposalTestHelper.AddNotVotedProposalIdList(notApprovedProposalIdList);
            var notApprovedPendingProposalIdList = new List<Hash>
            {
                proposalId3
            };
            _proposalTestHelper.AddNotVotedPendingProposalIdList(notApprovedPendingProposalIdList);
            
            var libHash = HashHelper.ComputeFrom("BlockHash");
            var libHeight = blockHeight;
            await _proposalService.ClearProposalByLibAsync(libHash, libHeight);
            var cachedProposalIdList = proposalCacheProvider.GetAllProposals();
            cachedProposalIdList.Count.ShouldBe(1);
            cachedProposalIdList.ShouldContain(proposalId3);
            
            var blockHash = HashHelper.ComputeFrom("BlockHash");
            var queryResultNotApprovedProposalIdList =
                await _proposalService.GetNotApprovedProposalIdListAsync(NormalAddress, blockHash, blockHeight);
            queryResultNotApprovedProposalIdList.Count.ShouldBe(1);
            queryResultNotApprovedProposalIdList.ShouldContain(proposalId3);
        }
    }
}