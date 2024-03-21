using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Kernel.CodeCheck.Application;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public class CodeCheckProposalProvider : ICodeCheckProposalProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<Hash, CodeCheckProposal> _proposalsToRelease = new();

    public void AddProposal(Hash proposalId, Hash proposalInputHash, long height)
    {
        var newProposal = new CodeCheckProposal
        {
            ProposalId = proposalId,
            ProposedContractInputHash = proposalInputHash,
            BlockHeight = height
        };

        _proposalsToRelease.AddOrUpdate(proposalId, newProposal,
            (hash, proposal) => proposal.BlockHeight >= height ? proposal : newProposal);
    }

    public IEnumerable<CodeCheckProposal> GetAllProposals()
    {
        return _proposalsToRelease.Values;
    }

    public bool TryGetProposalCreatedHeight(Hash proposalId, out long height)
    {
        if(_proposalsToRelease.TryGetValue(proposalId, out var proposal))
        {
            height = proposal.BlockHeight;
            return true;
        }

        height = 0;
        return false;
    }

    public void RemoveProposalById(Hash proposalId)
    {
        _proposalsToRelease.TryRemove(proposalId, out _);
    }
}