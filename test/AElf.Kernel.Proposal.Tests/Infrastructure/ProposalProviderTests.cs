using AElf.Kernel.Proposal.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Proposal.Tests.Infrastructure
{
    public class ProposalProviderTests : ProposalTestBase
    {
        private readonly IProposalProvider _proposalProvider;

        public ProposalProviderTests()
        {
            _proposalProvider = GetRequiredService<IProposalProvider>();
        }
        
        [Fact]
        public void GetAllProposalsTest()
        {
            var proposalId = HashHelper.ComputeFrom("ProposalId");
            var blockHeight = 10;
            var proposalIdList = _proposalProvider.GetAllProposals();
            proposalIdList.ShouldBeEmpty();
            
            _proposalProvider.AddProposal(proposalId, blockHeight);
            proposalIdList = _proposalProvider.GetAllProposals();
            proposalIdList.Count.ShouldBe(1);
            proposalIdList.ShouldContain(proposalId);

            var lowerHeight = blockHeight - 1;
            _proposalProvider.AddProposal(proposalId, lowerHeight);
            proposalIdList = _proposalProvider.GetAllProposals();
            proposalIdList.Count.ShouldBe(1);
            proposalIdList.ShouldContain(proposalId);
        }
        
        [Fact]
        public void AddProposalTest()
        {
            var proposalId = HashHelper.ComputeFrom("ProposalId");
            var blockHeight = 10;
            _proposalProvider.AddProposal(proposalId, blockHeight);
            var exist = _proposalProvider.TryGetProposalCreatedHeight(proposalId, out var height);
            exist.ShouldBeTrue();
            height.ShouldBe(blockHeight);

            var lowerHeight = blockHeight - 1;
            _proposalProvider.AddProposal(proposalId, lowerHeight);
            exist = _proposalProvider.TryGetProposalCreatedHeight(proposalId, out height);
            exist.ShouldBeTrue();
            height.ShouldBe(blockHeight);
        }

        [Fact]
        public void RemoveProposalByIdTest()
        {
            var proposalId = HashHelper.ComputeFrom("ProposalId");
            var blockHeight = 10;
            _proposalProvider.AddProposal(proposalId, blockHeight);
            _proposalProvider.RemoveProposalById(proposalId);
            var targetProposal = _proposalProvider.TryGetProposalCreatedHeight(proposalId, out _);
            targetProposal.ShouldBeFalse();
        }
    }
}