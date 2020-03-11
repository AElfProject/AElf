using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Proposal.Tests.Application
{
    public class ProposalServiceTests : ProposalTestBase
    {
        private readonly IProposalService _proposalService;
        private readonly ProposalTestHelper _proposalTestHelper;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ProposalServiceTests()
        {
            _proposalService = GetRequiredService<IProposalService>();
            _proposalTestHelper = GetRequiredService<ProposalTestHelper>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }

        [Fact]
        public void AddNotApprovedProposalTest()
        {
            var proposalCacheProvider = GetRequiredService<IReadyToApproveProposalCacheProvider>();

            {
                var proposalId = Hash.FromString("proposal");
                const int height = 100;
                _proposalService.AddNotApprovedProposal(proposalId, height);
                var exist = proposalCacheProvider.TryGetProposalCreatedHeight(proposalId, out var h);
                exist.ShouldBeTrue();
                h.ShouldBe(height);
            }

            {
                var proposalId = Hash.FromString("proposal");
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
            var proposalId1 = Hash.FromString("proposalId1");
            var proposalId2 = Hash.FromString("proposalId2");
            var proposalId3 = Hash.FromString("proposalId3");
            var proposalId4 = Hash.FromString("proposalId4");
            
            var proposalCacheProvider = GetRequiredService<IReadyToApproveProposalCacheProvider>();
            proposalCacheProvider.CacheProposalToApprove(proposalId1, 5);

            var notApprovedProposalIdList = new List<Hash>
            {
                proposalId1, proposalId2, proposalId3, proposalId4
            };
            _proposalTestHelper.AddNotVotedProposalIdList(notApprovedProposalIdList);
            
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider.Name);
            var blockHash = Hash.FromString("BlockHash");
            var blockHeight = 10;
            var queryResultNotApprovedProposalIdList =
                await _proposalService.GetNotApprovedProposalIdListAsync(NormalAddress, blockHash, blockHeight);
            queryResultNotApprovedProposalIdList.Count.ShouldBe(1);
            queryResultNotApprovedProposalIdList.ShouldContain(proposalId1);
        }
    }
}