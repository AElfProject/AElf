using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Tests;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public class CodeCheckProposalProviderTests : CodeCheckTestBase
{
    private readonly ICodeCheckProposalProvider _codeCheckProposalProvider;

    public CodeCheckProposalProviderTests()
    {
        _codeCheckProposalProvider = GetRequiredService<ICodeCheckProposalProvider>();
    }

    [Fact]
    public async Task ProposalTest()
    {
        var proposalId = HashHelper.ComputeFrom("ProposalId");
        var proposedContractInputHash = HashHelper.ComputeFrom("ProposedContractInputHash");
        var blockHeight = 10;
        var proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.ShouldBeEmpty();

        var getHeightResult = _codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out var createdHeight);
        getHeightResult.ShouldBeFalse();
        createdHeight.ShouldBe(0);

        _codeCheckProposalProvider.AddProposal(proposalId, proposedContractInputHash, blockHeight);
        proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.Count.ShouldBe(1);
        proposalList[0].BlockHeight.ShouldBe(blockHeight);
        proposalList[0].ProposalId.ShouldBe(proposalId);
        proposalList[0].ProposedContractInputHash.ShouldBe(proposedContractInputHash);
        
        getHeightResult = _codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out createdHeight);
        getHeightResult.ShouldBeTrue();
        createdHeight.ShouldBe(blockHeight);

        var lowerHeight = blockHeight - 1;
        _codeCheckProposalProvider.AddProposal(proposalId, proposedContractInputHash, lowerHeight);
        proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.Count.ShouldBe(1);
        proposalList[0].BlockHeight.ShouldBe(blockHeight);
        proposalList[0].ProposalId.ShouldBe(proposalId);
        proposalList[0].ProposedContractInputHash.ShouldBe(proposedContractInputHash);
        
        getHeightResult = _codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out createdHeight);
        getHeightResult.ShouldBeTrue();
        createdHeight.ShouldBe(blockHeight);
        
        var higherHeight = blockHeight + 1;
        _codeCheckProposalProvider.AddProposal(proposalId, proposedContractInputHash, higherHeight);
        proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.Count.ShouldBe(1);
        proposalList[0].BlockHeight.ShouldBe(higherHeight);
        proposalList[0].ProposalId.ShouldBe(proposalId);
        proposalList[0].ProposedContractInputHash.ShouldBe(proposedContractInputHash);
        
        getHeightResult = _codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out createdHeight);
        getHeightResult.ShouldBeTrue();
        createdHeight.ShouldBe(higherHeight);
        
        var newProposalId = HashHelper.ComputeFrom("NewProposalId");
        var newBlockHeight = 100;
        
        _codeCheckProposalProvider.AddProposal(newProposalId, proposedContractInputHash, newBlockHeight);
        proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.Count.ShouldBe(2);
        
        _codeCheckProposalProvider.RemoveProposalById(newProposalId);
        
        proposalList = _codeCheckProposalProvider.GetAllProposals();
        proposalList.Count.ShouldBe(1);
        proposalList[0].BlockHeight.ShouldBe(higherHeight);
        proposalList[0].ProposalId.ShouldBe(proposalId);
        proposalList[0].ProposedContractInputHash.ShouldBe(proposedContractInputHash);
    }
}